desktopdir 	 = $(datadir)/applications
desktop_in_files = pinta.desktop.in
desktop_DATA 	 = $(desktop_in_files:.desktop.in=.desktop)
@INTLTOOL_DESKTOP_RULE@

man_MANS = pinta.1

EXTRA_DIST =                                   \
       $(desktop_in_files)                     \
       $(man_MANS)

CLEANFILES = $(desktop_DATA)

DISTCLEANFILES = $(desktop_DATA)

