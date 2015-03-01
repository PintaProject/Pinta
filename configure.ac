dnl Process this file with autoconf to produce a configure script.
AC_INIT([pinta], [1.6])
AM_INIT_AUTOMAKE([tar-ustar foreign])


AC_PROG_INSTALL

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PATH_PROG(XBUILD, xbuild, no)
AC_PATH_PROG(MONO, mono, no)

if test "x$XBUILD" = "xno"; then
	AC_MSG_ERROR([XBuild not found])
fi

if test "x$MONO" = "xno"; then
	AC_MSG_ERROR([No mono runtime found])
fi

# Check Mono version
MONO_MINIMUM_VERSION=2.8
PKG_CHECK_MODULES(MONO, mono >= $MONO_MINIMUM_VERSION)

AC_SUBST(XBUILD)
AC_SUBST(MONO)

AC_PROG_INTLTOOL([0.35.0], [no-xml])
GETTEXT_PACKAGE=pinta
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE, "$GETTEXT_PACKAGE")

ALL_LINGUAS="af ar ast az be bg br bs ca cs da de el en_AU en_CA en_GB eo es et eu fi fil fo fr ga gl he hi hr hu hy id it ja ka ko la lt lv ms nb nl nn oc pl pt pt_BR ro ru sk sl sq sr sv th tr uk vi zh_CN zh_TW"
AM_GLIB_GNU_GETTEXT

pintalocaledir='${prefix}/${DATADIRNAME}/locale'
AC_SUBST(pintalocaledir)

PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])
PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0])
PKG_CHECK_MODULES([MONO_CAIRO], [mono-cairo])

AC_CONFIG_FILES([
xdg/Makefile
po/Makefile.in
pinta.pc
Makefile
pinta
])

AC_OUTPUT

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install Prefix:     	${prefix}
    XBuild Path:		${XBUILD}
    Mono Runtime:       	${MONO}
    Launch Script:		${prefix}/bin/${PACKAGE}
    Installation Directory: 	${prefix}/lib/${PACKAGE}/

EOF
