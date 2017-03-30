FROM winjre

COPY elasticsearch-5.0.2.zip c:/

RUN powershell Expand-Archive c:\elasticsearch-5.0.2.zip -DestinationPath c:/

COPY demo.zip c:/

RUN powershell Expand-Archive c:\demo.zip -DestinationPath c:/

# CMD [echo %JAVA_HOME%]

# CMD [c:\elasticsearch-5.0.2\bin\elasticsearch]

# EXPOSE 9200
# EXPOSE 9300
