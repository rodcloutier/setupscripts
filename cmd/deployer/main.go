package main

import (
	"fmt"
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
	target := filepath.Join(packagePath, tool.Path)
	for _, alias := range tool.Aliases {

		toolType := tool.Type
		if toolType == "" {
			toolType = "exe"
		}

		var createAlias func(string, string, string)

		switch tool.Type {
		// case "cmd":
		case "bash":
			createAlias = createBashAlias
		case "exe":
			createAlias = createExeAlias
		default:
			// TODO we could try to auto detect based on extension
			createAlias = createExeAlias
		}
		createAlias(binPath, alias, target)
	}
}

func createBashAlias(binPath string, alias string, targetPath string) {
	aliasPath := filepath.Join(binPath, alias)
	fmt.Printf("Creating bash alias %s targeting %s\n", aliasPath, targetPath)
}

func createExeAlias(binPath string, alias string, targetPath string) {
	aliasPath := filepath.Join(binPath, alias)
	fmt.Printf("Creating exe alias %s targeting %s\n", aliasPath, targetPath)
}

func newCloneOperation(installPath string, name string, toolset setupscripts.Toolset) (string, func()) {

	packageName := strings.Join([]string{name, toolset.Version}, ".")
	destinationPath := filepath.Join(installPath, packageName)
	clone := func() {
		fmt.Printf("git clone %s %s\n", toolset.Url, destinationPath)
	}

	return destinationPath, clone
}

func newDownloadOperation(installPath string, name string, toolset setupscripts.Toolset) (string, func()) {

	packageName := strings.Join([]string{name, toolset.Version}, ".")
	destinationPath := filepath.Join(installPath, packageName)

	parts := strings.Split(toolset.Url, "/")
	filename := parts[len(parts)-1]

	downloadPath := filepath.Join(installPath, filename)

	var deploy func()

	if strings.HasSuffix(filename, "exe") {
		deploy = func() {
			fmt.Printf("creating directory %s\n", destinationPath)
			fmt.Printf("copying %s to %s\n", downloadPath, destinationPath)
		}
	} else {
		deploy = func() {
			fmt.Printf("decompressing %s to %s\n", downloadPath, destinationPath)
		}
	}

	download := func() {
		fmt.Printf("downloading %s to %s\n", toolset.Url, downloadPath)
		deploy()
	}

	return destinationPath, download
}

func NewToolsetOp(installPath, name string, toolset setupscripts.Toolset) ToolsetOp {

	var path string
	op := func() {}

	// if toolset.url endswith .git => clone
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

	binPath := "/c/bin"
	installPath := "/c/bin/install"

	var operations []ToolsetOp

	for name, toolset := range deployment.Toolsets {
		operations = append(operations, NewToolsetOp(installPath, name, toolset))
	}

	// TODO make the operation list unique to avoid multiple download of the same file

	var wg sync.WaitGroup

	wg.Add(len(operations))
	for _, op := range operations {
		go func() {
			defer wg.Done()
			op.FetchPackage()
		}()
	}
	wg.Wait()

	// TODO make this parallel ?
	for _, op := range operations {
		op.CreateTools(binPath)
	}

	// TODO do post processing of tool for post deploy scripts

}
