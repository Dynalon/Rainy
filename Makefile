RELEASEVER=0.5.0
ZIPDIR=rainy-$(RELEASEVER)
BINDIR=$(shell pwd)/Rainy/bin/Release
RELEASEDIR=$(shell pwd)/release
SHELL=/bin/bash
MONO=$(shell which mono)
XBUILD=$(shell which xbuild)
LATEST_MASTER_COMMIT=$(shell git log -n 1 |head -n 1 | cut -f 2 -d ' ' |head -c 6)
NIGHTLY_DIR=rainy-nightly-$(LATEST_MASTER_COMMIT)
NIGHTLY_ZIP=rainy-nightly.zip

XBUILD_ARGS='/p:Configuration=Release'
MKBUNDLE=$(shell which mkbundle)

UNPACKED_EXE=$(BINDIR)/Rainy.exe
PACKED_EXE=Rainy.exe

# Note this is the min version for building from source; running might work
# on older mono versions
MIN_MONO_VERSION=3.0.0

pack: build
	@cp Rainy/settings.conf $(RELEASEDIR)/settings.conf
	@echo "Packing all assembly deps into the final .exe"
	$(MONO) ./tools/ILRepack.exe /out:$(RELEASEDIR)/$(PACKED_EXE) $(BINDIR)/Rainy.exe $(BINDIR)/*.dll
	@echo ""
	@echo "**********"
	@echo ""
	@echo "Success! Find your executable in $(RELEASEDIR)/$(PACKED_EXE)"
	@echo "To run rainy, copy $(RELEASEDIR)/$(PACKED_EXE) along with"
	@echo "the settings.conf to the desired location and run"
	@echo ""
	@echo "    mono Rainy.exe -c settings.conf"
	@echo ""
	@echo ""
	@echo "Please use https://github.com/Dynalon/Rainy to report any bugs!"
	@echo ""
	@echo "**********"
	@echo ""
	@echo ""

checkout:
ifndef TEAMCITY
	# Fetching Rainy's submodules
	@git submodule update --init --recursive
endif

deps: checkout
	# if the next steps fails telling about security authentication, make sure
	# you have imported trusted ssl CA certs with this command and re-run:
	#
	# mozroots --import --sync
	#

	@mono tools/NuGet.exe install -o packages Rainy/packages.config
	@mono tools/NuGet.exe install -o packages Rainy-tests/packages.config
	@pushd tomboy-library/ && make && popd
	@echo "Successfully fetched dependencies."

build: checkout deps

## this is not working?
##pkg-config --atleast-version=$(MIN_MONO_VERSION) mono; if [ $$? != "0" ]; then $(error "mono >=$MIN_MONO_VERSION is required");

	$(XBUILD) $(XBUILD_ARGS) Rainy.sln

release: clean pack
	cp -R $(RELEASEDIR) $(ZIPDIR)
	zip -r $(ZIPDIR).zip $(ZIPDIR)

nightly: clean pack
	cp -R $(RELEASEDIR) $(NIGHTLY_DIR)
	zip -r $(NIGHTLY_ZIP) $(NIGHTLY_DIR)

# statically linked binary
# does not require mono but will be > 13MB of size
linux_bundle: pack
	echo "Statically linking mono runtime to create .NET-free, self-sustained executable"
	mkdir -p $(RELEASEDIR)/linux/
	$(MKBUNDLE) --deps -z --static -o $(RELEASEDIR)/linux/rainy $(RELEASEDIR)/$(PACKED_EXE)


install: pack
	cp $(RELEASEDIR)/$(PACKED_EXE) /usr/bin/Rainy.exe

clean:
	rm -rf Rainy/obj/*
	rm -rf $(ZIPDIR)
	rm -rf $(ZIPDIR).zip
	rm -rf $(BINDIR)/*
	rm -rf $(NIGHTLY_DIR)
	rm -rf $(NIGHTLY_ZIP)
	rm -rf $(RELEASEDIR)/*.exe
	rm -rf $(RELEASEDIR)/*.mdb
	rm -rf $(RELEASEDIR)/data/
