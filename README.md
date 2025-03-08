# Fellow Oak DICOM for .NET (GMZ classic version)

### Features
* Targets .NET 4.8.1 and higher
* DICOM dictionary version 2015c
* High-performance, fully asynchronous API
* JPEG (including lossless), JPEG-LS, JPEG2000, and RLE image compression
* Supports very large datasets with content loading on demand
* Image rendering

### Examples

#### File Operations
```csharp
var file = DicomFile.Open(@"test.dcm");

var patientid = file.Dataset.Get<string>(DicomTag.PatientID);

file.Dataset.Add(DicomTag.PatientsName, "DOE^JOHN");

// creates a new instance of DicomFile
file = file.ChangeTransferSyntax(DicomTransferSyntax.JPEGProcess14SV1);

file.Save(@"output.dcm");
```

#### Render Image to JPEG
```csharp
var image = new DicomImage(@"test.dcm");
image.RenderImage().Save(@"test.jpg");
```

#### C-Store SCU
```csharp
var client = new DicomClient();
client.AddRequest(new DicomCStoreRequest(@"test.dcm"));
client.Send("127.0.0.1", 12345, false, "SCU", "ANY-SCP");
```

#### C-Echo SCU/SCP
```csharp
var server = new DicomServer<DicomCEchoProvider>(12345);

var client = new DicomClient();
client.NegotiateAsyncOps();
for (int i = 0; i < 10; i++)
    client.AddRequest(new DicomCEchoRequest());
client.Send("127.0.0.1", 12345, false, "SCU", "ANY-SCP");
```

#### C-Find SCU
```csharp
var cfind = DicomCFindRequest.CreateStudyQuery(patientId: "12345");
cfind.OnResponseReceived = (DicomCFindRequest rq, DicomCFindResponse rp) => {
	Console.WriteLine("Study UID: {0}", rp.Dataset.Get<string>(DicomTag.StudyInstanceUID));
};

var client = new DicomClient();
client.AddRequest(cfind);
client.Send("127.0.0.1", 104, false, "SCU-AE", "SCP-AE");
```

#### C-Move SCU
```csharp
var cmove = new DicomCMoveRequest("DEST-AE", studyInstanceUid);

var client = new DicomClient();
client.AddRequest(cmove);
client.Send("127.0.0.1", 104, false, "SCU-AE", "SCP-AE");
```

### Contributors
* [Colby Dillion](https://github.com/rcd)
* [Anders Gustafsson](https://github.com/anders9ustafsson) (Cureos AB)
* [Hesham Desouky](https://github.com/hdesouky) (Nebras Technology)
* [Ian Yates](http://github.com/IanYates)
* [Chris Horn](https://github.com/GMZ)
* [Mahesh Dubey](https://github.com/mdubey82)
* [Justin Wake](https://github.com/jwake)
* [captainstark](https://github.com/captainstark)

### License
This library is licensed under the [Microsoft Public License (MS-PL)](http://opensource.org/licenses/MS-PL). See _License.txt_ for more information.
