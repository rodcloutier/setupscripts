package main

import (
	"fmt"
	"os"
	"os/exec"
	"syscall"

	"github.com/jelmansouri/setupscripts"
)

func Execute(config setupscripts.Config, args ...string) (int, error) {

	cmd := exec.Command(config.Path, args...)

	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr

	env := os.Environ()
	for _, e := range config.Environment {
		env = append(env, e.Key+"="+e.Value)
	}
	cmd.Env = env

	if config.Blocking {
		if err := cmd.Run(); err != nil {
			// Did the command fail because of an unsuccessful exit code
			if exitError, ok := err.(*exec.ExitError); ok {
				fmt.Printf("%+v", err)
				waitStatus := exitError.Sys().(syscall.WaitStatus)
				return waitStatus.ExitStatus(), err
			}
			// Don't have an exit code, return a failure
			return 1, err
		}
		// Command was successful
		waitStatus := cmd.ProcessState.Sys().(syscall.WaitStatus)
		return waitStatus.ExitStatus(), nil
	}

	if err := cmd.Start(); err != nil {
		// TODO fetch proper error status
		fmt.Printf("%+v", err)
		return 1, err
	}
	return 0, nil
}

func main() {

	// read ReadConfig
	config, err := setupscripts.NewConfig(setupscripts.ConfigPathFromPath(os.Args[0]))
	if err != nil {
		panic(err)
	}

	// execute
	var exitCode int
	exitCode, err = Execute(*config, os.Args[1:]...)

	if err != nil {
		os.Exit(exitCode)
	}
}
