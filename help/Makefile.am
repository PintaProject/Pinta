@YELP_HELP_RULES@
HELP_ID = pinta

HELP_FILES = \
	     edit-crop.page \
	     index.page

HELP_MEDIA = \
	     figures/autocrop_after.png \
	     figures/autocrop_before.png \
	     figures/croptoselection_after.png \
	     figures/croptoselection_before.png

HELP_LINGUAS = br ca en_CA es ms sl

HTML_DIR = $(builddir)/html

# Installation directory for development builds.
BUILD_DIR = ../bin/help

updatepot: pot
	mv $(HELP_ID).pot $(HELP_ID)_help.pot

html: all
	rm -rf $(HTML_DIR)
	for lc in C $(HELP_LINGUAS); do \
	    echo "Building $$lc"; \
	    mkdir -p $(HTML_DIR)/$$lc; \
	    yelp-build html -o $(HTML_DIR)/$$lc $(builddir)/$$lc; \
	done

copyhelp: all
	mkdir -p $(BUILD_DIR)
	for lc in C $(HELP_LINGUAS); do \
	    echo "Installing help for $$lc"; \
	    srcdir=$$lc; \
	    helpdir=$(BUILD_DIR)/$$lc; \
	    mkdir -p $$helpdir; \
	    \
	    for f in $(HELP_FILES); do \
	    	$(INSTALL_DATA) $$srcdir/$$f $$helpdir/$$f; \
	    done ;\
	    \
	    for f in $(HELP_MEDIA); do \
	    	dname=`dirname "$$f"`; \
		mkdir -p $$helpdir/$$dname; \
	    	if test -f "$$srcdir/$$f"; then \
		    echo "$(INSTALL_DATA) $$srcdir/$$f $$helpdir/$$f"; \
		    $(INSTALL_DATA) $$srcdir/$$f $$helpdir/$$f; \
	    	elif test "$$lc" != "C"; then \
		    echo "$(INSTALL_DATA) C/$$f $$helpdir/$$f"; \
		    $(INSTALL_DATA) C/$$f $$helpdir/$$f; \
		fi; \
	    done ;\
	done

clean-local:
	rm -rf $(HTML_DIR)
