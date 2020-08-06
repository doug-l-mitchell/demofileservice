## Demo File Service ##


This project demonstrates how to manage uploading, downloading, listing and deleting files in a webservice. The files are stored in Azure Blob Storage. This demo uses a storage emulator but the service can be configured to point to the Azure cloud.

### FileController ###

This controller contains endpoints to support the features. Unit tests focus on the REST method response behavior, as there isn't really any data validation or business logic contained in this example. The *IFileRepository* dependency is injected and mocked in tests.

There are opportunities for improvements to the FileController unit tests. I had found that the MemoryStream was being closed before the asynchronous method returned, which failed with an exception. This is missed in the unit tests and found when manually testing toward the end of my work. 

Further enhancements (if this were a real world application) could be adding metric collection through filter attributes as this is a cross cutting concern. For example, count how often files are uploaded, downloaded, deleted.


### FileRepository ###

The FileRepository is the link to Blob Storage in this case. The IFileRepository interface is generic enough, I believe, so that it could point to other storage types (file, AWS S3, Google Drive, etc). The BlobStorageClient is injected into the concrete class and mocked in tests. I tried to ensure that Blob Storage details do not leak past the repository layer so exceptions are caught, should be logged and appropriate action taken. I like the approach of not catching exceptions that you know know how to handle. The WebApi framework handles uncaught exceptions by returning a 500 Server Error, which I feel should be reserved explicitly for that situation (500 errors should cause us to take quick action as something unexpected happened).

### Blob Storage Emulator ###

The Storage account is configurable in the appsettings file or can be overridden on the environment. This demo works with a storage account in Azure but I have used azurite running in docker. 