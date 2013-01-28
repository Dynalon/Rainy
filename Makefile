RELEASEVER=0.1
ZIPDIR=rainy-$(RELEASEVER)
BINDIR=$(shell pwd)/Rainy/bin/Debug
RELEASEDIR=$(shell pwd)/release
TMPDIR=$(shell pwd)/.tmp

MONO=$(shell which mono)
XBUILD=$(shell which xbuild)
MKBUNDLE=$(shell which mkbundle)

UNPACKED_EXE=$(BINDIR)/Rainy.exe
PACKED_EXE=Rainy.exe
MIN_MONO_VERSION=2.10.9


build: prepare
	$(XBUILD) Rainy.sln
pack: prepare build
	echo "Packing all assembly deps into the final .exe"
	$(MONO) ./tools/ILRepack.exe /out:$(RELEASEDIR)/$(PACKED_EXE) $(BINDIR)/Rainy.exe $(BINDIR)/*.dll

prepare:
# this is not working?
#pkg-config --atleast-version=$(MIN_MONO_VERSION) mono; if [ $$? != "0" ]; then $(error "mono >=2.10.9 is required");

	# rainy's submodules
	@git submodule init
	@git submodule update

	# tomboy-library submodules
	@cd tomboy-library/ && git submodule init && git submodule update && cd ..

release: clean pack
	# create a new tag
	cp Rainy/settings.conf $(RELEASEDIR)/settings.conf
	rm -rf *.zip
	cp -R $(RELEASEDIR) $(ZIPDIR)
	zip -r $(ZIPDIR).zip $(ZIPDIR)
	
# Packed CIL image, only requires mono to run
# but not any deps

linux_bundle: pack
	echo "Statically linking mono runtime to create .NET-free, self-sustained executable"
	mkdir -p $(RELEASEDIR)/linux/
	$(MKBUNDLE) -z --static -o $(RELEASEDIR)/linux/rainy $(RELEASEDIR)/$(PACKED_EXE)

clean:
	rm -rf $(ZIPDIR)
	rm -rf $(ZIPDIR).zip
	rm -rf $(TMPDIR)
	rm -rf $(BINDIR)/*
	rm -rf $(RELEASEDIR)/*
