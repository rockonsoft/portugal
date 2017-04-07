# Portugal Retreat

## Docker
To simply run the nanoserver container in interactive mode:
```
docker run -it microsoft/nanoserver powershell
```
Once inside powershell, execute the following to show OS details
(Paste into command window not quite working)

```
gcim Win32_OperatingSystem | select Version, InstallDate, OSArchitecture
```
### Build JRE
The JRE images installs java and set some PATH variables.
To build the images from the docker file: [C:\src\demo-portugal>]
```
docker build -t winjre .\portugal\docker\windev-jre-8u91
```
The output for the build:
```
Sending build context to Docker daemon 57.67 MB
Step 1/6 : FROM microsoft/windowsservercore
 ---> b4713e4d8bab
Step 2/6 : COPY jre-8u91-windows-x64.exe c:/
 ---> d233bda03981
Removing intermediate container 7abcbd4a90c8
Step 3/6 : RUN powershell start-process -filepath C:\jre-8u91-windows-x64.exe -passthru -wait -argumentlist "/s,INSTALLDIR=c:\Java\jre1.8.0_91,/L,install64.log"
 ---> Running in e4c60944f38d

Handles  NPM(K)    PM(K)      WS(K)     CPU(s)     Id  SI ProcessName
-------  ------    -----      -----     ------     --  -- -----------
     45       4      708      35924       0.45   1736   1 jre-8u91-windows-x64

 ---> 7f6da18298a2
Removing intermediate container e4c60944f38d
Step 4/6 : CMD c:\Java\jre1.8.0_91\bin\java.exe -version
 ---> Running in a8cec674e71a
 ---> b3e21187b8e6
Removing intermediate container a8cec674e71a
Step 5/6 : ENV JAVA_HOME "c:\Java\jre1.8.0_91"
 ---> Running in 20d6b6a67757
 ---> fcc9a4f147f1
Removing intermediate container 20d6b6a67757
Step 6/6 : CMD echo %JAVA_HOME%
 ---> Running in e67f77c24877
 ---> 6fdd27d10cc4
Removing intermediate container e67f77c24877
Successfully built 6fdd27d10cc4
```

Once the build completed
```
docker run -it winjre powershell
```
And in the interactive shell: ```Get-ChildItem Env:```

### Build ES

The elasticsearch image is based on the JRE image.
To build the ES images (wines):[C:\src\demo-portugal>]
``` 
docker build -t wines  .\portugal\docker\windev-elasticsearch
```
With output:
```
Sending build context to Docker daemon 90.66 MB
Step 1/7 : FROM winjre
 ---> 6fdd27d10cc4
Step 2/7 : COPY elasticsearch-5.0.2.zip c:/
 ---> Using cache
 ---> 8699f1e6a452
Step 3/7 : RUN powershell Expand-Archive c:\elasticsearch-5.0.2.zip -DestinationPath c:/
 ---> Using cache
 ---> 3b6208c89d81
Step 4/7 : CMD [echo %JAVA_HOME%]
 ---> Running in 6a32cfdd5d3f
 ---> 74a4e4ca26d6
Removing intermediate container 6a32cfdd5d3f
Step 5/7 : CMD [c:\elasticsearch-5.0.2\bin\elasticsearch]
 ---> Running in 2a9c48602e2f
 ---> 4970ab1e76fd
Removing intermediate container 2a9c48602e2f
Step 6/7 : EXPOSE 9200
 ---> Running in 277b9f0d3c8b
 ---> f97f482c436a
Removing intermediate container 277b9f0d3c8b
Step 7/7 : EXPOSE 9300
 ---> Running in 225b2d45e15b
 ---> e1a748139534
Removing intermediate container 225b2d45e15b
Successfully built e1a748139534
```

### Running elastic
 
 To run elastic you have to give the contianer more memory:
 ```
 docker run -it -m 4g -p 9200:9200 wines powershell
 ```
 Once inside the container, strart the elasticsearch and test it,
 taking into account that es takes some time to start.
```
 C:\elasticsearch-5.0.2\bin\elasticsearch
 curl -Uri http://localhost:9200/_cat/indices?v -UseBasicParsing
```

 PS C:\Debug> .\DemoEventSource.exe
******************** CustomizedEventSource Demo ********************
Sending a variety of messages, including 'Start', an 'Stop' Messages.
  Event RequestStart (0, /home/index.aspx).
  Event RequestPhase (0, initialize).
  Event RequestPhase (0, query_db).
  Event RequestPhase (0, query_webservice).
  Event RequestPhase (0, send_results).
  Event RequestStop (0).
  Event RequestStart (1, /home/catalog/100).
  Event RequestPhase (1, initialize).
  Event RequestPhase (1, query_db).
  Event RequestPhase (1, query_webservice).
  Event RequestPhase (1, process_results).
  Event RequestPhase (1, send_results).
  Event RequestStop (1).
  Event RequestStart (2, /home/catalog/121).
  Event RequestPhase (2, initialize).
  Event RequestPhase (2, query_db).
  Event DebugTrace (Error on page: /home/catalog/121).
  Event RequestStop (2).
  Event RequestStart (3, /home/catalog/144).
  Event RequestPhase (3, initialize).
  Event RequestPhase (3, query_db).
  Event RequestPhase (3, query_webservice).
  Event RequestPhase (3, process_results).
  Event RequestPhase (3, send_results).
  Event RequestStop (3).
Done generating events.

PS C:\Debug> wget -Uri http://localhost:9200/_cat/indices?v -UseBasicParsing


StatusCode        : 200
StatusDescription : OK
Content           : health status index                            uuid                   pri rep docs.count
                    docs.deleted store.size pri.store.size
                    yellow open   lisbon-retreat-eae30b358afb-1224 p2NlqA27QeaYgvAbsHQlIQ   ...
RawContent        : HTTP/1.1 200 OK
                    Content-Type: text/plain; charset=UTF-8

                    health status index                            uuid                   pri rep docs.count
                    docs.deleted store.size pri.st...
Forms             :
Headers           : {[Content-Length, 256], [Content-Type, text/plain; charset=UTF-8]}
Images            : {}
InputFields       : {}
Links             : {}
ParsedHtml        :
RawContentLength  : 256



PS C:\Debug> wget -Uri http://localhost:9200/_cat/indices?v -UseBasicParsing


StatusCode        : 200
StatusDescription : OK
Content           : health status index                            uuid                   pri rep docs.count
                    docs.deleted store.size pri.store.size
                    yellow open   lisbon-retreat-eae30b358afb-1224 p2NlqA27QeaYgvAbsHQlIQ   ...
RawContent        : HTTP/1.1 200 OK

                    health status index                            uuid                   pri rep docs.count
                    docs.deleted store.size pri.st...
Forms             :
Headers           : {[Content-Length, 256], [Content-Type, text/plain; charset=UTF-8]}
Images            : {}
ParsedHtml        :
RawContentLength  : 256



PS C:\Debug> wget -Uri http://localhost:9200/_search
wget : The response content cannot be parsed because the Internet Explorer engine is not available, or Internet
    + CategoryInfo          : NotImplemented: (:) [Invoke-WebRequest], NotSupportedException
    + FullyQualifiedErrorId : WebCmdletIEDomNotSupportedException,Microsoft.PowerShell.Commands.InvokeWebRequestComman
   d

PS C:\Debug> wget -Uri http://localhost:9200/lisbon-retreat-eae30b358afb-1224/_search?q='initialize' -UseBasicParsing


Content           : {"took":62,"timed_out":false,"_shards":{"total":5,"successful":5,"failed":0},"hits":{"total":4,"max
                    _score":1.8236469,"hits":[{"_index":"lisbon-retreat-eae30b358afb-1224","_type":"event","_id":"ff55b
                    86...
RawContent        : HTTP/1.1 200 OK
                    Content-Length: 1819
                    Content-Type: application/json; charset=UTF-8

                    {"took":62,"timed_out":false,"_shards":{"total":5,"successful":5,"failed":0},"hits":{"total":4,"max
                    _score":1.823...
Forms             :
Headers           : {[Content-Length, 1819], [Content-Type, application/json; charset=UTF-8]}
Images            : {}
InputFields       : {}
Links             : {}
ParsedHtml        :
RawContentLength  : 1819



PS C:\Debug> wget -Uri http://localhost:9200/lisbon-retreat-eae30b358afb-1224/_search?q='initialize' -UseBasicParsing |
select Content

Content
-------
{"took":1,"timed_out":false,"_shards":{"total":5,"successful":5,"failed":0},"hits":{"total":4,"max_score":1.8236469,...


PS C:\Debug> wget -Uri http://localhost:9200/lisbon-retreat-eae30b358afb-1224/_search?q='initialize' -UseBasicParsing |
select Content | ConvertFrom-Json
ConvertFrom-Json : Invalid JSON primitive: .
At line:1 char:125
+ ... h?q='initialize' -UseBasicParsing | select Content | ConvertFrom-Json
+                                                          ~~~~~~~~~~~~~~~~
    + CategoryInfo          : NotSpecified: (:) [ConvertFrom-Json], ArgumentException
    + FullyQualifiedErrorId : System.ArgumentException,Microsoft.PowerShell.Commands.ConvertFromJsonCommand

PS C:\Debug> wget -Uri http://localhost:9200/lisbon-retreat-eae30b358afb-1224/_search?q='initialize' -UseBasicParsing |
select Content | pretty
PS C:\Debug> wget -Uri http://localhost:9200/lisbon-retreat-eae30b358afb-1224/_search?q='initialize' -UseBasicParsing |
select Content | ConvertFrom-Json
ConvertFrom-Json : Invalid JSON primitive: .
At line:1 char:125
+ ... h?q='initialize' -UseBasicParsing | select Content | ConvertFrom-Json
+                                                          ~~~~~~~~~~~~~~~~
    + CategoryInfo          : NotSpecified: (:) [ConvertFrom-Json], ArgumentException
    + FullyQualifiedErrorId : System.ArgumentException,Microsoft.PowerShell.Commands.ConvertFromJsonCommand

PS C:\Debug> wget -Uri http://localhost:9200/lisbon-retreat-eae30b358afb-1224/_search?q='initialize' -UseBasicParsing |
select Content

Content
-------
{"took":16,"timed_out":false,"_shards":{"total":5,"successful":5,"failed":0},"hits":{"total":4,"max_score":1.8236469...


PS C:\Debug>