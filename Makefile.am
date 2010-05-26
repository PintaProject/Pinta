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

# target: clean - Default clean command: cleanobj and distclean
clean: distclean cleanobj

# target: cleanall - Removes build files. 
cleanall: distclean cleanobj cleanbin
	rm -rf autom4te.cache
	rm -f configure aclocal.m4 missing install-sh Makefile.in

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
	$(INSTALL_DATA) -t $(prefix)/lib/$(PACKAGE)/ $(BINFILES)
	$(INSTALL_DATA) -t $(prefix)/lib/$(PACKAGE)/Extensions/ $(EXTENSIONS)
	$(INSTALL_SCRIPT) pinta $(prefix)/bin/$(PACKAGE)

# target: uninstall - Uninstalls Pinta.
uninstall: 
	rm -vf $(prefix)/bin/$(PACKAGE)
	rm -rvf $(prefix)/lib/$(PACKAGE)

# target: release - Default release type: releasetar
release: releasetar

# target: releasetar - Make a release tarball.
releasetar: $(BINFILES) $(EXTENSIONS)
	cd bin && tar -czf ../$(RELEASE_FILE).tgz --exclude=*mdb *

# target: releasezip - Make a release zip file.
releasezip: $(BINFILES) $(EXTENSIONS)
	cd bin && zip -r ../$(RELEASE_FILE).zip * -x "*.mdb"

.PHONY: install uninstall cleanall cleanbin cleanobj release releasetar releasezip
