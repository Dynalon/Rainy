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

pack: prepare build
	echo "Packing all assembly deps into the final .exe"
	$(MONO) ./tools/ILRepack.exe /out:$(RELEASEDIR)/$(PACKED_EXE) $(BINDIR)/Rainy.exe $(BINDIR)/*.dll

build: prepare
	$(XBUILD) Rainy.sln

prepare:
## this is not working?
##pkg-config --atleast-version=$(MIN_MONO_VERSION) mono; if [ $$? != "0" ]; then $(error "mono >=2.10.9 is required");

	# Fetching Rainy's submodules
	@git submodule init
	@git submodule update

	# Fetching tomboy-library's submodules
	@cd tomboy-library/ && git submodule init && git submodule update && cd ..

release: clean pack
	cp Rainy/settings.conf $(RELEASEDIR)/settings.conf
	rm -rf *.zip
	cp -R $(RELEASEDIR) $(ZIPDIR)
	zip -r $(ZIPDIR).zip $(ZIPDIR)
	
# statically linked binary
# does not require mono but will be > 13MB of size
linux_u: pack
	echo "Statically linking mono runtime to create .NET-free, self-sustained executable"
	mkdir -p $(RELEASEDIR)/linux/
	$(MKBUNDLE) -z --static -o $(RELEASEDIR)/linux/rainy $(RELEASEDIR)/$(PACKED_EXE)

clean:
	rm -rf $(ZIPDIR)
	rm -rf $(ZIPDIR).zip
	rm -rf $(TMPDIR)
	rm -rf $(BINDIR)/*
	rm -rf $(RELEASEDIR)/*.exe
	rm -rf $(RELEASEDIR)/*.mdb
