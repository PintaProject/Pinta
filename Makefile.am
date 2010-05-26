BINFILES = \
	bin/Pinta.exe \
	bin/Pinta.Core.dll \
	bin/Pinta.Gui.Widgets.dll \
	bin/Pinta.Resources.dll \
	bin/System.ComponentModel.Composition.dll
	
EXTENSIONS = \
	bin/Extensions/Pinta.Effects.dll \
	bin/Extensions/Pinta.Tools.dll

RELEASE_FILE = $(PACKAGE)-$(VERSION)

# target: all - Default target: build
all: build

# target: help - Display callable targets.
help:
	egrep "^# target:" Makefile | sed 's/^# target:/make/'

# target: build - Build Pinta.
build: Pinta.sln
	$(XBUILD) Pinta.sln

# target: list - List source files.
list:
	if ! git status > /dev/null 2> /dev/null ;\
	then \
		find . -type f -name *.cs | sed 's|./||' ;\
	else \
		git ls-files | grep '\.cs' ;\
	fi

# target: clean - Default clean command: cleanobj
clean: cleanobj

# target: cleanall - Removes build files. 
cleanall: distclean cleanobj cleanbin

# target: cleanbin - Removes built files. 
cleanbin: 
	rm -rvf bin/*

# target: cleanobj - Removes temporary build files. 
cleanobj:
	find . -type d -name obj | xargs rm -rvf
	
# target: install - Installs Pinta. 
install: pinta $(BINFILES) $(EXTENSIONS)
	mkdir -p $(prefix)/bin
	mkdir -p $(prefix)/lib/$(PACKAGE)
	mkdir -p $(prefix)/lib/$(PACKAGE)/Extensions
	install -v -m 555 -t $(prefix)/lib/$(PACKAGE)/ $(BINFILES)
	install -v -m 555 -t $(prefix)/lib/$(PACKAGE)/Extensions/ $(EXTENSIONS)
	install -v -m 555 pinta $(prefix)/bin/$(PACKAGE)

# target: uninstall - Uninstalls Pinta.
uninstall: 
	rm -vf $(prefix)/bin/$(PACKAGE)
	rm -rvf $(prefix)/lib/$(PACKAGE)

# target: dist - Default distribution type: disttar
dist: disttar

# target: disttar - Make a release tarball.
disttar: $(BINFILES) $(EXTENSIONS)
	cd bin && tar -czf ../$(RELEASE_FILE).tgz --exclude=*mdb *

# target: distzip - Make a release zip file.
distzip: $(BINFILES) $(EXTENSIONS)
	cd bin && zip -r ../$(RELEASE_FILE).zip * -x "*.mdb"

.PHONY: install uninstall cleanall cleanbin cleanobj disttar distzip
