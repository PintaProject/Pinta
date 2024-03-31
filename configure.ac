dnl Process this file with autoconf to produce a configure script.
AC_INIT([pinta], [2.2])
AM_INIT_AUTOMAKE([tar-ustar foreign])

AC_PROG_INSTALL

dnl pkg-config
PKG_PROG_PKG_CONFIG([0.20])

AC_PATH_PROG(DOTNET, dotnet, no)

if test "x$DOTNET" = "xno"; then
    AC_MSG_ERROR([No dotnet runtime found])
fi

# Check dotnet version
DOTNET_MINIMUM_VERSION=7.0
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

GETTEXT_PACKAGE=pinta
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE, "$GETTEXT_PACKAGE")

AM_GNU_GETTEXT([external])
AM_GNU_GETTEXT_REQUIRE_VERSION([0.19.7])

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
