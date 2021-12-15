SUBDIRS = xdg
DIST_SUBDIRS = xdg

BINDIR = $(srcdir)/bin

PINTA_BUILD_OPTS = --configuration Release -p:BuildTranslations=true

PINTA_INSTALL_OPTS = \
	-p:SourceDir=$(abs_srcdir) \
	-p:PublishDir=$(abs_srcdir)/publish \
	-p:InstallPrefix=$(DESTDIR)$(prefix) \
	-p:InstallBinDir=$(DESTDIR)$(bindir) \
	-p:InstallLibDir=$(DESTDIR)$(libdir) \
	-p:InstallDataRootDir=$(DESTDIR)$(datarootdir) \
	-p:InstallManDir=$(DESTDIR)$(mandir) \
	-p:InstallLocaleDir=$(DESTDIR)$(localedir)

PINTA_ASSEMBLY_PC = $(srcdir)/$(PACKAGE_NAME).pc

RELEASE_FILE = $(PACKAGE)-$(VERSION)

#magic automake variables

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = $(PINTA_ASSEMBLY_PC)

#end

# target: all - Default target: build
all: build

# target: run - Launch the uninstalled copy
run: build
	$(DOTNET) bin/Pinta.dll

# target: help - Display callable targets.
help:
	egrep "^# target:" Makefile | sed 's/^# target:/make/'

# target: build - Build Pinta.
build: Pinta.sln
	cd $(srcdir) && $(DOTNET) build Pinta.sln $(PINTA_BUILD_OPTS)

test: build
	$(DOTNET) test Pinta.sln $(PINTA_BUILD_OPTS)

# target: list - List source files.
list:
	if ! git status > /dev/null 2> /dev/null ;\
	then \
		find . -type f -name *.cs | sed 's|./||' ;\
	else \
		git ls-files | grep '\.cs' ;\
	fi

#target: updatepotfiles - update the POTFILES.in file
updatepotfiles:
	echo xdg/pinta.desktop.in > po/POTFILES.in
	echo xdg/pinta.appdata.xml.in >> po/POTFILES.in
	find -s \
		Pinta \
		Pinta.Core \
		Pinta.Docking \
		Pinta.Effects \
		Pinta.Gui.Widgets \
		Pinta.Resources \
		Pinta.Tools \
		-name \*.cs -not -path '*/obj/*' >> po/POTFILES.in

#target: updatepot - update the messages.pot file
updatepot:
	cd po && intltool-update -m && intltool-update -p --gettext-package=messages
	cat po/messages.in >> po/messages.pot
	cat po/messages.pot | msguniq -s -o po/messages.pot

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
install-exec-local: build
	$(DOTNET) publish Pinta.sln $(PINTA_BUILD_OPTS) --self-contained false -o $(srcdir)/publish
	$(DOTNET) msbuild installer/linux/install.proj -target:Install $(PINTA_INSTALL_OPTS)
	$(INSTALL_SCRIPT) installer/linux/pinta $(DESTDIR)$(bindir)/$(PACKAGE)

# target: uninstall - Uninstalls Pinta.
uninstall-hook:
	$(DOTNET) msbuild installer/linux/install.proj -target:Uninstall $(PINTA_INSTALL_OPTS)

# target: releasezip - Make a self-contained release zip file.
releasezip:
	$(DOTNET) publish Pinta.sln $(PINTA_BUILD_OPTS) -r linux-x64 --self-contained true -o $(srcdir)/release
	cd $(srcdir)/release && zip -r ../$(RELEASE_FILE).zip * -x "*.pdb"

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

.PHONY: install uninstall cleanbin cleanobj
