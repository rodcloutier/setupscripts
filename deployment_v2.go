package setupscripts

import (
	"gopkg.in/yaml.v2"
	"io/ioutil"
)

type Deployment struct {
	Version string
	// BinPath     string
	// InstallPath string
	// HttpProxy   string

	Toolsets map[string]Toolset
}

// type Command struct {
// 	FilePath    string
// 	Arguments   string
// 	Environment []EnvironmentVar `yaml:"envVariables"`
// }

type Toolset struct {
	Url     string
	Version string
	Tools   []Tool
}

type Tool struct {
	Path    string
	Type    string
	Aliases []string
}

func NewDeployment(path string) (*Deployment, error) {

	file, err := ioutil.ReadFile(path)
	if err != nil {
		return nil, err
	}

	deployment := new(Deployment)
	err = yaml.Unmarshal(file, deployment)
	if err != nil {
		return nil, err
	}
	return deployment, nil
}
