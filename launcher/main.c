#include <shlobj.h>
#include <shlwapi.h>
#include <objbase.h>
#include <shellapi.h>
#include <windows.h>
#include <stdio.h>

#define LAUNCHER_MAX_COMMAND_SIZE       2048
#define LAUNCHER_MAX_PATH               (MAX_PATH + 1)
#define LAUNCHER_MAX_OPTION_SIZE        128
#define LAUNCHER_MAX_ENV_VAR_SIZE       1024

char gs_AppName[LAUNCHER_MAX_PATH] = {'F', 'I', 'L', 'L', 'A', 'P', 'P', 'N', 'A', 'M', 'E'};
char gs_Options[LAUNCHER_MAX_OPTION_SIZE] = {'F', 'I', 'L', 'L', 'O', 'P', 'T', 'I', 'O', 'N', 'S'};
char gs_EnvVariable[LAUNCHER_MAX_ENV_VAR_SIZE] = {'F', 'I', 'L', 'L', 'E', 'N', 'V', 'V', 'A', 'R', 'S'};
//char gs_AppName[LAUNCHER_MAX_PATH] = "C:\\DevTools\\cpython-3.5.2.amd64\\python.exe";
//char gs_Options[LAUNCHER_MAX_OPTION_SIZE] = "nonblocking,nostdredirect";
//char gs_EnvVariable[LAUNCHER_MAX_ENV_VAR_SIZE] = "GOROOT=C:\\DevTools\\go1.7.4.windows-amd64\\go,TEST1=YayYay";

int main(int argc, char* argv[]) {
    char currentDir[LAUNCHER_MAX_PATH];
    GetCurrentDirectoryA(LAUNCHER_MAX_PATH, currentDir);

    char cmdLine[LAUNCHER_MAX_COMMAND_SIZE] = {0};
    for (int i = 1; i < argc; ++i) {
        strcat_s(cmdLine, LAUNCHER_MAX_COMMAND_SIZE, " ");
        strcat_s(cmdLine, LAUNCHER_MAX_COMMAND_SIZE, argv[i]);
    }

    BOOL blocking = TRUE;
    BOOL stdRedirect = TRUE;
    if (strlen(gs_Options)) {
        char *nextToken;
        char* token = strtok_s(gs_Options, ",", &nextToken);
        while (token != NULL) {
            if (strcmp(token, "nonblocking")) {
                blocking = FALSE;
            }
            else if (strcmp(token, "nostdredirect")) {
                stdRedirect = FALSE;
            }
            token = strtok_s(NULL, ",", &nextToken);
        }
    }

    if (strlen(gs_EnvVariable)) {
        char *nextToken;
        char* token = strtok_s(gs_EnvVariable, ",", &nextToken);
        while (token != NULL) {
            char* equalChar = strchr(token, '=');
            if (equalChar) {
                *equalChar = '\0';
                equalChar++;
                if (!SetEnvironmentVariableA(token, equalChar))
                {
                    break;
                }
            }
            token = strtok_s(NULL, ",", &nextToken);
        }
    }
    

    STARTUPINFOA startInfo;
    memset(&startInfo, 0, sizeof(startInfo));
    startInfo.cb = sizeof(startInfo);
    if (stdRedirect) {
        startInfo.dwFlags = STARTF_USESTDHANDLES;
        startInfo.hStdInput = GetStdHandle(STD_INPUT_HANDLE);
        startInfo.hStdOutput = GetStdHandle(STD_OUTPUT_HANDLE);
        startInfo.hStdError = GetStdHandle(STD_ERROR_HANDLE);
    }
    
    PROCESS_INFORMATION procInfo;
    memset(&procInfo, 0, sizeof(procInfo));

    CreateProcessA(
        gs_AppName,
        cmdLine,
        NULL,
        NULL,
        TRUE,
        0,
        NULL,
        currentDir,
        &startInfo,
        &procInfo
    );

    // Wait until child process exits.
    if (blocking) {
        return WaitForSingleObject(procInfo.hProcess, INFINITE);
    }
    return 0;
}