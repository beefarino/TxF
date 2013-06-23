# TxF : Transactional FileSystem PowerShell Provider

This PowerShell module enables you to leverage transactional file system operations from PowerShell.

## Supported Operations

At the moment, the following standard provider cmdlets support transactional operations in the TxF Provider:

*   new-item
*   remove-item
*   get-item
*   get-childitem

## Planned Support

The following standard provider cmdlets are planned to support transaction operations in the TxF Provider:

*   get-content
*   set-content
*   rename-item
*   move-item
*   copy-item

# QuickStart

	> import-module txf
	
	> # define a new drive based on the TxF PowerShell Provider.
	> # note the root of the drive points to an existing file system path	
	> new-psdrive cx -psp txf -root c:\

	WARNING: column "CurrentLocation" does not fit into the display and was removed.
	
	Name           Used (GB)     Free (GB) Provider      Root
	----           ---------     --------- --------      ----
	cx                                     TxF           c:\
	
	> # start a new transaction	
	> start-transaction
		
	> # create a new transactional file
	> new-item cx:\share\data.txt -type file -value 'testing' -usetransaction
	
	    Directory: TxF::c:\share
	
	
	Mode                LastWriteTime     Length Name
	----                -------------     ------ ----
	darhs        12/31/1600   7:00 PM            data.txt
	
	
	> # note the file is not listed when the -usetransaction parameter is omitted from the 
	> # get-childitem provider cmdlet
	> ls cx:\share
	
	    Directory: TxF::c:\share
	
	
	Mode                LastWriteTime     Length Name
	----                -------------     ------ ----
	
	
	> # but appears when -usetransaction is present
	> ls cx:\share -usetransaction
		
	
	    Directory: TxF::c:\share
	
	
	Mode                LastWriteTime     Length Name
	----                -------------     ------ ----
	darhs        12/31/1600   7:00 PM            data.txt
	
	> # complete the transaction
	> Complete-Transaction
	
	> # now the file exists outside of the transactional scope
	> ls cx:\share
	
	
	    Directory: TxF::c:\share
	
	
	Mode                LastWriteTime     Length Name
	----                -------------     ------ ----
	-a---         6/22/2013   8:03 PM          7 data.txt