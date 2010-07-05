dnl Process this file with autoconf to produce a configure script.
AC_INIT([pinta], [0.4])
AM_INIT_AUTOMAKE([foreign])

AC_PREFIX_DEFAULT([/usr])
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

AC_SUBST(XBUILD)
AC_SUBST(MONO)

PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])
PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0])
PKG_CHECK_MODULES([MONO_CAIRO], [mono-cairo])

AC_CONFIG_FILES([
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
