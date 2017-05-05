package main

import (
	"fmt"
	"io"
	"io/ioutil"
	"log"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"sync"

	"github.com/jelmansouri/setupscripts"
)

type ToolsetOp struct {
	Toolset      setupscripts.Toolset
	PackagePath  string
	FetchPackage func()
}

func (toolsetOp *ToolsetOp) CreateTools(binPath string) {

	for _, tool := range toolsetOp.Toolset.Tools {
		createTool(binPath, toolsetOp.PackagePath, tool)
	}
}

func createTool(binPath, packagePath string, tool setupscripts.Tool) {

	toolPath := strings.Replace(tool.Path, "\\", "/", -1)
	targetPath := filepath.Clean(filepath.Join(packagePath, toolPath))
	for _, alias := range tool.Aliases {

		aliasPath := filepath.Clean(filepath.Join(binPath, alias))

		// TODO handle launcher (GUI) type which would use launcher app
		// to start in non blocking mode
		fmt.Printf("link %s -> %s\n", alias, targetPath)

		err := os.Symlink(targetPath, aliasPath)
		if err != nil {
			log.Fatalln(err)
		}
	}
}

func newCloneOperation(installPath string, name string, toolset setupscripts.Toolset) (string, func()) {

	packageName := strings.Join([]string{name, toolset.Version}, ".")
	destinationPath := filepath.Join(installPath, packageName)
	clone := func() {
		fmt.Printf("git clone %s %s\n", toolset.Url, destinationPath)
	}

	return destinationPath, clone
}

func downloadFile(url, destinationPath string) {
	fmt.Printf("downloading %s\n", url)
	response, e := http.Get(url)
	if e != nil {
		fmt.Println("%s", e)
		log.Fatal(e)
	}

	defer response.Body.Close()

	//open a file for writing
	file, err := os.Create(destinationPath)
	if err != nil {
		fmt.Println("%s", e)
		log.Fatal(err)
	}
	_, err = io.Copy(file, response.Body)
	if err != nil {
		fmt.Println("%s", e)
		log.Fatal(err)
	}
	file.Close()
	fmt.Printf("done downloading %s\n", url)
}

func newDownloadOperation(installPath string, name string, toolset setupscripts.Toolset) (string, func()) {

	packageName := strings.Join([]string{name, toolset.Version}, ".")
	destinationPath := filepath.Join(installPath, packageName)

	err := os.MkdirAll(destinationPath, 0755)
	if err != nil {
		log.Fatal(err)
	}

	parts := strings.Split(toolset.Url, "/")
	filename := parts[len(parts)-1]

	var deploy func(string)

	if strings.HasSuffix(filename, "exe") {
		deploy = func(downloadPath string) {
			os.Mkdir(destinationPath, 0755)
			filePath := filepath.Join(destinationPath, filename)
			os.Rename(downloadPath, filePath)
		}
	} else {
		deploy = func(downloadPath string) {
			fmt.Printf("decompressing %s\n", downloadPath)
			Unzip(downloadPath, destinationPath)
		}
	}

	download := func() {
		downloadPath := filepath.Join(installPath, filename)

		downloadFile(toolset.Url, downloadPath)

		deploy(downloadPath)

		fmt.Printf("removing temp %s\n", downloadPath)
		os.Remove(downloadPath)
	}

	return destinationPath, download
}

func NewToolsetOp(installPath, name string, toolset setupscripts.Toolset) ToolsetOp {

	var path string
	op := func() {}

	if strings.HasSuffix(toolset.Url, ".git") {
		path, op = newCloneOperation(installPath, name, toolset)
	} else {
		path, op = newDownloadOperation(installPath, name, toolset)
	}

	return ToolsetOp{Toolset: toolset, PackagePath: path, FetchPackage: op}
}

func main() {
	deployment, err := setupscripts.NewDeployment(os.Args[1])
	if err != nil {
		panic(err)
	}

	binPath, err := ioutil.TempDir("", "example")
	if err != nil {
		log.Fatal(err)
	}
	fmt.Printf("binPath = %s\n", binPath)

	installPath := filepath.Join(binPath, "install")

	err = os.MkdirAll(installPath, 0755)
	if err != nil {
		panic(err)
	}

	var operations []ToolsetOp

	for name, toolset := range deployment.Toolsets {
		operations = append(operations, NewToolsetOp(installPath, name, toolset))
	}

	// TODO make the operation list unique to avoid multiple download of the same file

	var wg sync.WaitGroup

	wg.Add(len(operations))
	for _, op := range operations {
		go func(o ToolsetOp) {
			defer wg.Done()
			o.FetchPackage()
		}(op)
	}
	wg.Wait()

	// TODO make this parallel ?
	for _, op := range operations {
		op.CreateTools(binPath)
	}

	// TODO do post processing of tool for post deploy scripts

}
