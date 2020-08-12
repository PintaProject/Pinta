SUBDIRS = xdg
DIST_SUBDIRS = xdg

BINDIR = $(srcdir)/bin

PINTA_ASSEMBLY_PC = $(srcdir)/$(PACKAGE_NAME).pc

BINFILES = \
	$(BINDIR)/Pinta.exe \
	$(BINDIR)/Pinta.Core.dll \
	$(BINDIR)/Pinta.Docking.dll \
	$(BINDIR)/Pinta.Gui.Widgets.dll \
	$(BINDIR)/Pinta.Resources.dll \
	$(BINDIR)/Pinta.Effects.dll \
	$(BINDIR)/Pinta.Tools.dll

DEBUG_FILES = $(addsuffix .mdb, $(BINFILES))

RELEASE_FILE = $(PACKAGE)-$(VERSION)

MAC_APP_DIR = Pinta.app
MAC_APP_BIN_DIR = "$(MAC_APP_DIR)/Contents/MacOS/"

#magic automake variables

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = $(PINTA_ASSEMBLY_PC)

#end

# target: all - Default target: build
all: build

# target: run - Launch the uninstalled copy
run:
	$(MONO) bin/Pinta.exe
	
# target: help - Display callable targets.
help:
	egrep "^# target:" Makefile | sed 's/^# target:/make/'

# target: build - Build Pinta.
build: Pinta.sln
	cd $(srcdir) && $(XBUILD) Pinta.sln
	cd $(srcdir) && $(XBUILD) Pinta.Install.proj /target:CompileTranslations

test: build
	$(XBUILD) tests/Pinta.Core.Tests/Pinta.Core.Tests.csproj
	nunit-console bin/Pinta.Core.Tests.dll

# target: list - List source files.
list:
	if ! git status > /dev/null 2> /dev/null ;\
	then \
		find . -type f -name *.cs | sed 's|./||' ;\
	else \
		git ls-files | grep '\.cs' ;\
	fi

#target: updatepot - update the messages.pot file
updatepot:
	cd po && intltool-update -m && intltool-update -p --gettext-package=messages
	cat po/messages.in >> po/messages.pot
	cat po/messages.pot | msguniq -o po/messages.pot

# target: clean - Default clean command: cleanobj and cleanbin
clean-local: cleanobj cleanbin

maintainer-clean-local:
	rm -rf autom4te.cache

# target: cleanbin - Removes built files. 
cleanbin: 
	rm -rvf $(BINDIR)/*

# target: cleanobj - Removes temporary build files. 
cleanobj:
	find $(srcdir)/ -type d -name obj | xargs rm -rvf
	
# target: install - Installs Pinta. 
install-exec-local: pinta $(BINFILES)
	cd $(srcdir) && $(XBUILD) /target:Install /property:InstallPrefix=$(DESTDIR)$(prefix) \
		/property:BinDir=$(DESTDIR)$(bindir) /property:LibDir=$(DESTDIR)$(libdir) \
		/property:DataRootDir=$(DESTDIR)$(datarootdir) /property:ManDir=$(DESTDIR)$(mandir) \
		/property:LocaleDir=$(DESTDIR)$(localedir) Pinta.Install.proj
	$(INSTALL_SCRIPT) pinta $(DESTDIR)$(bindir)/$(PACKAGE)

# target: uninstall - Uninstalls Pinta.
uninstall: 
	$(XBUILD) /target:Uninstall /property:InstallPrefix=$(DESTDIR)$(prefix) Pinta.Install.proj

# target: release - Default release type: releasetar
release: releasetar

# target: releasetar - Make a release tarball.
releasetar: $(BINFILES)
	cd $(BINDIR) && tar -czf ../$(RELEASE_FILE).tgz --exclude=*mdb *

# target: releasezip - Make a release zip file.
releasezip: $(BINFILES)
	cd $(BINDIR) && zip -r ../$(RELEASE_FILE).zip * -x "*.mdb"

releaseosx: build $(BINFILES)
	mkdir -p $(MAC_APP_DIR)/Contents/{MacOS,Resources}
	cp -r $(BINDIR)/locale $(BINFILES) $(DEBUG_FILES) $(BINDIR)/ICSharpCode.SharpZipLib.dll osx/pinta $(MAC_APP_BIN_DIR)
	chmod +x $(MAC_APP_BIN_DIR)/pinta
	cp osx/Info.plist $(MAC_APP_DIR)/Contents
	cp osx/pinta.icns $(MAC_APP_DIR)/Contents/Resources
	touch $(MAC_APP_DIR)
	zip -r9uq $(MAC_APP_DIR).zip $(MAC_APP_DIR)
	rm -rf $(MAC_APP_DIR)

EXTRA_DIST = Pinta Pinta.Core Pinta.Docking Pinta.Effects Pinta.Gui.Widgets Pinta.Resources Pinta.Tools po xdg tests license-mit.txt \
           license-pdn.txt Pinta.sln Pinta.Install.proj pinta.pc.in readme.md intltool-extract.in \
           intltool-merge.in intltool-update.in

CLEANFILES = intltool-extract \
	     intltool-update \
	     intltool-merge \
	     intltool-extract.in \
	     intltool-update.in \
	     intltool-merge.in

DISTCLEANFILES = po/.intltool-merge-cache \
		 po/Makefile \
		 po/Makefile.in \
		 po/Makefile

MAINTAINERCLEANFILES = po/Makefile.in.in \
		       Makefile.in \
		       install-sh \
		       configure \
		       aclocal.m4 \
		       missing \
		       compile

.PHONY: install uninstall cleanbin cleanobj release releasetar releasezip
