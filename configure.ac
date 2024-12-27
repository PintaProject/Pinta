dnl Process this file with autoconf to produce a configure script.
AC_INIT([pinta], [3.0])
AM_INIT_AUTOMAKE([tar-ustar foreign])

AC_PROG_INSTALL

dnl pkg-config
PKG_PROG_PKG_CONFIG([0.20])

AC_PATH_PROG(DOTNET, dotnet, no)

if test "x$DOTNET" = "xno"; then
    AC_MSG_ERROR([No dotnet runtime found])
fi

# Provide a more useful error message if AX_COMPARE_VERSION is not found.
m4_ifndef([AX_COMPARE_VERSION],
          [AC_MSG_ERROR([AX_COMPARE_VERSION macro not found. You may need to install the 'autoconf-archive' package])]
)

# Check dotnet version
DOTNET_MINIMUM_VERSION=8.0
DOTNET_VERSION=$($DOTNET --version)
AX_COMPARE_VERSION([$DOTNET_VERSION], [ge], [$DOTNET_MINIMUM_VERSION],
    AC_MSG_NOTICE([Found dotnet $DOTNET_VERSION]),
    AC_MSG_ERROR([dotnet $DOTNET_MINIMUM_VERSION or higher is required (found version $DOTNET_VERSION).])
)

# Select target framework based on the dotnet version.
DOTNET_MAJOR_VERSION=$(echo $DOTNET_VERSION | cut -d '.' -f 1,2)
DOTNET_TARGET_FRAMEWORK="net$DOTNET_MAJOR_VERSION"
AC_MSG_NOTICE([Using target framework $DOTNET_TARGET_FRAMEWORK])

DOTNET_CMD="env TargetFramework=$DOTNET_TARGET_FRAMEWORK $DOTNET"

AC_SUBST(DOTNET_CMD)

AC_PROG_INTLTOOL([0.35.0], [no-xml])
GETTEXT_PACKAGE=pinta
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE, "$GETTEXT_PACKAGE")

# Note: also need to update installer/macos/Info.plist when adding a language.
ALL_LINGUAS="af ar ast az be bg bn br bs ca cs da de dv el en_AU en_CA en_GB eo es et eu fa fi fil fo fr ga gl he hi hr hu hy id ie it ja ka kab ko la lt lv ms my nb nl nn oc pl pt pt_BR ro ru si sk sl sq sr sv th tr uk vi zh_CN zh_HK zh_TW"
AM_GLIB_GNU_GETTEXT

pintalocaledir='${prefix}/${DATADIRNAME}/locale'
AC_SUBST(pintalocaledir)

PKG_CHECK_MODULES([ADW_1], [libadwaita-1 >= 1.4])
PKG_CHECK_MODULES([GTK], [gtk4 >= 4.12])

AC_CONFIG_FILES([
Makefile
installer/linux/pinta
pinta.pc
po/Makefile.in
xdg/Makefile
])

AC_OUTPUT

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install Prefix:     	${prefix}
    .NET Runtime:       	${DOTNET}
    .NET Target Framework:      ${DOTNET_TARGET_FRAMEWORK}
    Launch Script:	        ${prefix}/bin/${PACKAGE}
    Installation Directory: ${prefix}/lib/${PACKAGE}/

EOF
