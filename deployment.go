package setupscripts

import (
	"gopkg.in/yaml.v2"
	"io/ioutil"
)

type Deployment struct {
	BinPath     string
	InstallPath string
	HttpProxy   string

	Repositories []Repository
	Packages     []Package
	Toolsets     []Toolset
}

type Repository struct {
	Id     string
	Type   string
	Source string
}

type Package struct {
	Id       string
	Version  string
	SourceID string
	Commands []Command
}

type Command struct {
	FilePath    string
	Arguments   string
	Environment []EnvironmentVar `yaml:"envVariables"`
}

type Toolset struct {
	Id          string
	PackageSpec string
	Tools       []Tool
}

type Tool struct {
	Path        string
	Type        string
	Blocking    bool
	Environment []EnvironmentVar `yaml:"envVariables"`
	Aliases     []string
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
