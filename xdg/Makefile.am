desktopdir 	 = $(datadir)/applications
desktop_in_files = pinta.desktop.in
desktop_DATA 	 = $(desktop_in_files:.desktop.in=.desktop)
@INTLTOOL_DESKTOP_RULE@

appdatadir = $(datadir)/appdata
appdata_in_files = pinta.appdata.xml.in
appdata_DATA = $(appdata_in_files:.xml.in=.xml)
@INTLTOOL_XML_RULE@

man_MANS = pinta.1

EXTRA_DIST =                                   \
       $(desktop_in_files)                     \
       $(appdata_in_files)                     \
       $(man_MANS)

CLEANFILES = $(desktop_DATA) $(appdata_DATA)

DISTCLEANFILES = $(desktop_DATA) $(appdata_DATA)

MAINTAINERCLEANFILES = Makefile.in
