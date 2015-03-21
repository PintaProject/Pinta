@YELP_HELP_RULES@
HELP_ID = pinta
HELP_FILES = index.page
HELP_MEDIA =
HELP_LINGUAS = en_CA

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
