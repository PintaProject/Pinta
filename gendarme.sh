#! /bin/sh

# Gendarme - Rule-based code analysis for Mono C#
gendarme --log log.txt --xml log.xml  --html log.html --severity critical --confidence total ./bin/Pinta.*.dll ./bin/Pinta.exe