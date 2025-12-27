SUBDIRS = xdg
DIST_SUBDIRS = xdg

BUILDDIR = $(srcdir)/build
BINDIR = $(BUILDDIR)/bin

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

# target: all - Default target: publish
all: publish

# target: run - Launch the uninstalled copy
run: build
	$(DOTNET_CMD) build/bin/Pinta.dll

# target: help - Display callable targets.
help:
	egrep "^# target:" Makefile | sed 's/^# target:/make/'

build/bin/Pinta.dll: Pinta.sln
	cd $(srcdir) && $(DOTNET_CMD) build Pinta.sln $(PINTA_BUILD_OPTS)

# target: build - Build Pinta.
build: build/bin/Pinta.dll

# Note we only publish the Pinta project, not the whole solution, to avoid races publishing to the same folder (#1241)
publish/Pinta.dll: Pinta.sln
	cd $(srcdir) && $(DOTNET_CMD) publish Pinta/Pinta.csproj $(PINTA_BUILD_OPTS) --self-contained false -p:PublishDir=$(abs_srcdir)/publish

# target: publish - Build Pinta for deployment.
publish: publish/Pinta.dll

test: build
	$(DOTNET_CMD) test Pinta.sln $(PINTA_BUILD_OPTS)

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
	echo xdg/com.github.PintaProject.Pinta.desktop.in > po/POTFILES.in
	echo xdg/com.github.PintaProject.Pinta.metainfo.xml.in >> po/POTFILES.in
	find \
		Pinta \
		Pinta.Core \
		Pinta.Docking \
		Pinta.Effects \
		Pinta.Gui.Addins \
		Pinta.Gui.Widgets \
		Pinta.Resources \
		Pinta.Tools \
		-name \*.cs -not -path '*/obj/*' | sort >> po/POTFILES.in

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
	rm -rvf $(BUILDDIR)/*

# target: cleanobj - Removes temporary build files.
cleanobj:
	find $(srcdir)/ -type d -name obj | xargs rm -rvf

# target: install - Installs Pinta.
install-exec-local: publish
	$(DOTNET_CMD) msbuild installer/linux/install.proj -target:Install $(PINTA_INSTALL_OPTS)
	$(INSTALL_SCRIPT) installer/linux/pinta $(DESTDIR)$(bindir)/$(PACKAGE)

# target: uninstall - Uninstalls Pinta.
uninstall-hook:
	$(DOTNET_CMD) msbuild installer/linux/install.proj -target:Uninstall $(PINTA_INSTALL_OPTS)

# target: releasezip - Make a self-contained release zip file.
releasezip:
	$(DOTNET_CMD) publish Pinta/Pinta.csproj $(PINTA_BUILD_OPTS) -r linux-x64 --self-contained true -p:PublishDir=$(abs_srcdir)/release
	cd $(abs_srcdir)/release && zip -r ../$(RELEASE_FILE).zip * -x "*.pdb"

EXTRA_DIST = Pinta Pinta.Core Pinta.Docking Pinta.Effects Pinta.Gui.Addins Pinta.Gui.Widgets Pinta.Resources Pinta.Tools po xdg tests license-mit.txt \
           license-pdn.txt Pinta.sln pinta.pc.in readme.md intltool-extract.in \
           intltool-merge.in intltool-update.in installer/addins installer/linux/install.proj properties Directory.Build.props Directory.Packages.props

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
