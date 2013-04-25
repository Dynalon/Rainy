RELEASEVER=0.2.3
ZIPDIR=rainy-$(RELEASEVER)
BINDIR=$(shell pwd)/Rainy/bin/Debug
RELEASEDIR=$(shell pwd)/release
TMPDIR=$(shell pwd)/.tmp

MONO=$(shell which mono)
XBUILD=$(shell which xbuild)

#XBUILD_ARGS='/p:TargetFrameworkProfile=""'
MKBUNDLE=$(shell which mkbundle)

UNPACKED_EXE=$(BINDIR)/Rainy.exe
PACKED_EXE=Rainy.exe
MIN_MONO_VERSION=2.10.9

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

build: 
## this is not working?
##pkg-config --atleast-version=$(MIN_MONO_VERSION) mono; if [ $$? != "0" ]; then $(error "mono >=2.10.9 is required");

	# Fetching Rainy's submodules
	@git submodule init
	@git submodule update --recursive

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
	rm -rf $(TMPDIR)
	rm -rf $(BINDIR)/*
	rm -rf $(RELEASEDIR)/*.exe
	rm -rf $(RELEASEDIR)/*.mdb
	rm -rf $(RELEASEDIR)/data/
