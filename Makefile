RELEASEVER=0.2.3
ZIPDIR=rainy-$(RELEASEVER)
BINDIR=$(shell pwd)/Rainy/bin/Debug
RELEASEDIR=$(shell pwd)/release

MONO=$(shell which mono)
XBUILD=$(shell which xbuild)

#XBUILD_ARGS='/p:TargetFrameworkProfile=""'
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

deps:
	# if the next steps fails telling about security authentication, make sure
	# you have imported trusted ssl CA certs with this command and re-run:
	#
	# mozroots --import --sync
	#

	@mono tools/NuGet.exe install -o packages Rainy/packages.config
	@mono tools/NuGet.exe install -o packages Rainy-tests/packages.config
	@mono tools/NuGet.exe install -o packages tomboy-library-websync/packages.config
	@echo "Successfully fetched dependencies."

build: checkout deps

## this is not working?
##pkg-config --atleast-version=$(MIN_MONO_VERSION) mono; if [ $$? != "0" ]; then $(error "mono >=$MIN_MONO_VERSION is required");

	$(XBUILD) $(XBUILD_ARGS) Rainy.sln

release: clean pack
	cp -R $(RELEASEDIR) $(ZIPDIR)
	zip -r $(ZIPDIR).zip $(ZIPDIR)
	
# statically linked binary
# does not require mono but will be > 13MB of size
linux_bundle: pack
	echo "Statically linking mono runtime to create .NET-free, self-sustained executable"
	mkdir -p $(RELEASEDIR)/linux/
	$(MKBUNDLE) -z --static -o $(RELEASEDIR)/linux/rainy $(RELEASEDIR)/$(PACKED_EXE)

clean:
	rm -rf Rainy/obj/*
	rm -rf $(ZIPDIR)
	rm -rf $(ZIPDIR).zip
	rm -rf $(BINDIR)/*
	rm -rf $(RELEASEDIR)/*.exe
	rm -rf $(RELEASEDIR)/*.mdb
	rm -rf $(RELEASEDIR)/data/
