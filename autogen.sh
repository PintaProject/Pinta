#! /bin/sh

PROJECT=Pinta
FILE=Pinta.sln
CONFIGURE=configure.ac

: ${AUTORECONF=autoreconf}

DIE=0

($AUTORECONF --version) < /dev/null > /dev/null 2>&1 || {
        echo
        echo "You must have autoconf installed to compile $PROJECT."
        echo "Download the appropriate package for your distribution,"
        echo "or get the source tarball at ftp://ftp.gnu.org/pub/gnu/"
        DIE=1
}

if test "$DIE" -eq 1; then
        exit 1
fi

# Check directory.
srcdir=`dirname $0`
test -z "$srcdir" && srcdir=.

ORIGDIR=`pwd`
cd $srcdir
TEST_TYPE=-f
aclocalinclude="-I . $ACLOCAL_FLAGS"

test $TEST_TYPE $FILE || {
        echo "You must run this script in the top-level $PROJECT directory"
        exit 1
}

if test -z "$*"; then
        echo "I am going to run ./configure with no arguments - if you wish "
        echo "to pass any to it, please specify them on the $0 command line."
fi

echo "Running $AUTORECONF ..."
$AUTORECONF --install

echo Running $srcdir/configure $conf_flags "$@" ...
$srcdir/configure $conf_flags "$@"
