# ClipboardShared
A Windows clipboard sharing (between the host and virtual machines) application using a local shared folder.

I am using VMWare Workstation 15 Player, unfortunitely it doesn't support clipboard sharing between host and guest. It's a known issue and there is really no way around it. VirtualBox has good support for it, but I had trouble setting it to work with an Xbox controller, which is a requirement for my job.
This in the long run, I hope to rewrite it using Electron and google drive service. For now it serves my own purpose.

Need to specify a directory as a cmdline argument, eg. "D:\clipboardShared"