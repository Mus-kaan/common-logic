Please put the files you want to access on the packer machine under this folder.

For example, you can move your private build artifacts to this folder before building and packing the image builder project.

The `bake-image.sh` is a required file and cannot be removed. It is the entry point to run on the packer VM.

It is recommended to edit `bake-image.sh` on a **Linux** dev box and generate the tar file on **Linux** box. If you are using windows to do this, it will encounter all kinds of issues.