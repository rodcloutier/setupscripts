package setupscripts

import (
	"encoding/json"
	"io/ioutil"
	"path/filepath"
	"strings"
)

type Config struct {
	Path        string           `json:"exePath"`
	Blocking    bool             `json:"blocking,omitempty"`
	Environment []EnvironmentVar `json:"envVariables,omitempty"`
}

type EnvironmentVar struct {
	Key   string
	Value string
}

func NewConfig(path string) (*Config, error) {

	file, err := ioutil.ReadFile(path)
	if err != nil {
		return nil, err
	}

	config := new(Config)
	err = json.Unmarshal(file, config)
	if err != nil {
		return nil, err
	}
	return config, nil
}

func ConfigPathFromPath(path string) string {
	basePath, _ := SplitExt(path)
	return basePath + ".cfg"
}

// TODO move to utils
func SplitExt(path string) (string, string) {
	ext := filepath.Ext(path)
	return strings.Replace(path, ext, "", 1), ext
}
