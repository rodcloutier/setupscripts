package main

import (
	"fmt"
	"os"

	"github.com/jelmansouri/setupscripts"
)

func main() {
	deployment, err := setupscripts.NewDeployment(os.Args[1])
	if err != nil {
		panic(err)
	}
	fmt.Println("%+v", deployment)
}
