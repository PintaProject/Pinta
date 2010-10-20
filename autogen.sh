#! /bin/sh

PROJECT=Pinta
FILE=
CONFIGURE=configure.ac

: ${AUTOCONF=autoconf}
: ${AUTOHEADER=autoheader}
: ${AUTOMAKE=automake}
: ${LIBTOOLIZE=libtoolize}
: ${ACLOCAL=aclocal}
: ${LIBTOOL=libtool}

srcdir=`dirname $0`
test -z "$srcdir" && srcdir=.

ORIGDIR=`pwd`
cd $srcdir
TEST_TYPE=-f
aclocalinclude="-I . $ACLOCAL_FLAGS"

DIE=0

($AUTOCONF --version) < /dev/null > /dev/null 2>&1 || {
        echo
        echo "You must have autoconf installed to compile $PROJECT."
        echo "Download the appropriate package for your distribution,"
        echo "or get the source tarball at ftp://ftp.gnu.org/pub/gnu/"
        DIE=1
}

($AUTOMAKE --version) < /dev/null > /dev/null 2>&1 || {
        echo
        echo "You must have automake installed to compile $PROJECT."
        echo "Get ftp://sourceware.cygnus.com/pub/automake/automake-1.4.tar.gz"
        echo "(or a newer version if it is available)"
        DIE=1
}

(grep "^AM_PROG_LIBTOOL" $CONFIGURE >/dev/null) && {
  ($LIBTOOL --version) < /dev/null > /dev/null 2>&1 || {
    echo
    echo "**Error**: You must have \`libtool' installed to compile $PROJECT."
    echo "Get ftp://ftp.gnu.org/pub/gnu/libtool-1.2d.tar.gz"
    echo "(or a newer version if it is available)"
    DIE=1
  }
}

if test "$DIE" -eq 1; then
        exit 1
fi
                                                                                
#test $TEST_TYPE $FILE || {
#        echo "You must run this script in the top-level $PROJECT directory"
#        exit 1
#}

if test -z "$*"; then
        echo "I am going to run ./configure with no arguments - if you wish "
        echo "to pass any to it, please specify them on the $0 command line."
fi

case $CC in
*xlc | *xlc\ * | *lcc | *lcc\ *) am_opt=--include-deps;;
esac

(grep "^AM_PROG_LIBTOOL" $CONFIGURE >/dev/null) && {
    echo "Running $LIBTOOLIZE ..."
    $LIBTOOLIZE --force --copy
}

echo "Running $ACLOCAL $aclocalinclude ..."
$ACLOCAL $aclocalinclude

echo "Running $AUTOMAKE --gnu $am_opt ..."
$AUTOMAKE --add-missing --gnu $am_opt

echo "Running $AUTOCONF ..."
$AUTOCONF

echo Running $srcdir/configure $conf_flags "$@" ...
$srcdir/configure --enable-maintainer-mode $conf_flags "$@" \
