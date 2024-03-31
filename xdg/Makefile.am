desktopdir 	 = $(datadir)/applications
desktop_in_files = pinta.desktop.in
desktop_DATA 	 = $(desktop_in_files:.desktop.in=.desktop)

%.desktop: %.desktop.in
	msgfmt --desktop -d $(top_srcdir)/po --template $< -o $@

appdatadir = $(datadir)/metainfo
appdata_in_files = pinta.appdata.xml.in
appdata_DATA = $(appdata_in_files:.xml.in=.xml)

%.appdata.xml: %.appdata.xml.in
	msgfmt --xml -d $(top_srcdir)/po --template $< -o $@

man_MANS = pinta.1

EXTRA_DIST =                                   \
       $(desktop_in_files)                     \
       $(appdata_in_files)                     \
       $(man_MANS)

CLEANFILES = $(desktop_DATA) $(appdata_DATA)

DISTCLEANFILES = $(desktop_DATA) $(appdata_DATA)

MAINTAINERCLEANFILES = Makefile.in
