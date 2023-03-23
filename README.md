## Overview 

This is a simple console app that connets via SFTP to two file servers then retrieves a file list from a given directory (which is hardcoded into the application but will be changed in the future). Once the file list is loaded, we then iterate through the files to locate files of type txt (again this will be changed as needed) then we modfify it. As this was built to transform a specific file whose type and structure are currently unknown, we do some simple string modifications currently. We will need to update the tranformation method later. After the transformation, we then upload the file to the second SFTP server under the same name. If part of the transformation throws an exception then we log the error and once all files have attempted to be processed we ask the user if they want to retry the ones that failed. If they choose not to retry the failed files then the failed files are recorded in a text file and they can be retried next time the application is opened. 

A new log is created each time the application is launched.

![Animation](https://user-images.githubusercontent.com/54787437/227081207-4390df6f-0bb1-44c2-91b1-c0c8ce6e5bb3.gif)
