# TxF : Transactional FileSystem PowerShell Provider

This PowerShell module enables you to leverage transactional file system operations from PowerShell.

## Supported Operations

At the moment, the following standard provider cmdlets support transactional operations in the TxF Provider:

*   new-item
*   remove-item
*   get-item
*   get-childitem
*   set-content
*   get-content

## Planned Support

The following standard provider cmdlets are planned to support transaction operations in the TxF Provider:

*   move-item
*   copy-item

# QuickStart

	> import-module txf
	
	> # the TxF module automatically defines new drives that correspond 
	> # to each filesystem drive avaiable in your session.
    > # these drives are named after their non-transactional filesystem drives,
    > # prefaced with an 'x'.
    > # for instance, the XC: drive represents the transaction-enabled C: drive.
	
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
	
	> # update the file content as part of the current transaction
	> set-content cx:\share\data.txt -value 'this will be the new content of the file' -usetransaction
	
    > # complete the transaction
	> Complete-Transaction
	
	> # now the file exists outside of the transactional scope
	> ls cx:\share
	
	
	    Directory: TxF::c:\share
	
	
	Mode                LastWriteTime     Length Name
	----                -------------     ------ ----
	-a---         6/22/2013   8:03 PM          7 data.txt

    > # the file content persists past the current transaction
	> get-content cx:\share\data.txt 
    this will be the new content of the file
	