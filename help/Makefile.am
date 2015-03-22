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

# Use relative paths for the symlinks for non-localized images.
LN_S += -r

HELP_LINGUAS = br ca en_CA es ms sl

HTML_DIR = $(builddir)/html

updatepot: pot
	mv $(HELP_ID).pot $(HELP_ID)_help.pot

html: all
	rm -rf $(HTML_DIR)
	for lc in C $(HELP_LINGUAS); do \
	    echo "Building $$lc"; \
	    mkdir -p $(HTML_DIR)/$$lc; \
	    yelp-build html -o $(HTML_DIR)/$$lc $(builddir)/$$lc; \
	done

clean-local:
	rm -rf $(HTML_DIR)
