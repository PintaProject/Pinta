SUBDIRS = xdg

BINDIR = $(srcdir)/bin

PINTA_ASSEMBLY_PC = $(srcdir)/$(PACKAGE_NAME).pc

BINFILES = \
	$(BINDIR)/Pinta.exe \
	$(BINDIR)/Pinta.Core.dll \
	$(BINDIR)/Pinta.Gui.Widgets.dll \
	$(BINDIR)/Pinta.Resources.dll \
	$(BINDIR)/Pinta.Effects.dll \
	$(BINDIR)/Pinta.Tools.dll

RELEASE_FILE = $(PACKAGE)-$(VERSION)

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

# target: list - List source files.
list:
	if ! git status > /dev/null 2> /dev/null ;\
	then \
		find . -type f -name *.cs | sed 's|./||' ;\
	else \
		git ls-files | grep '\.cs' ;\
	fi

#target: updatepot - update the messages.pot file
updatepot: build
	find . -type f -name *.cs | sed 's|./||' > files.list
	grep -v System.ComponentModel.Composition files.list > files2.list
	xgettext --from-code=UTF-8 -f files2.list -o po/messages.pot
	cat po/messages.in >> po/messages.pot
	rm files.list
	rm files2.list

# target: clean - Default clean command: cleanobj and cleanbin
clean: cleanobj cleanbin

# target: distclean - Removes configure files. 
distclean: clean
	rm -f Makefile pinta config.status config.cache config.log configure.lineno config.status.lineno pinta.pc

# target: cleanall - Removes build files. 
cleanall: distclean
	rm -rf autom4te.cache
	rm -f configure aclocal.m4 missing install-sh Makefile.in

# target: cleanbin - Removes built files. 
cleanbin: 
	rm -rvf $(BINDIR)/*

# target: cleanobj - Removes temporary build files. 
cleanobj:
	find $(srcdir)/ -type d -name obj | xargs rm -rvf
	
# target: install - Installs Pinta. 
install: pinta $(BINFILES)
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

EXTRA_DIST = Pinta Pinta.Core Pinta.Effects Pinta.Gui.Widgets Pinta.Resources Pinta.Tools po xdg license-mit.txt \
           license-pdn.txt todo.txt Pinta.sln Pinta.Install.proj pinta.pc.in readme.md intltool-extract.in \
           intltool-merge.in intltool-update.in

CLEANFILES = \
       intltool-extract \
       intltool-update \
       intltool-merge

DISTCLEANFILES = intltool-extract \
                 intltool-merge \
                 intltool-update \
                 po/.intltool-merge-cache



.PHONY: install uninstall cleanall cleanbin cleanobj release releasetar releasezip
