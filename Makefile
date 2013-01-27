all:
	# rainy's submodules
	@git submodule init
	@git submodule update

	# tomboy-library submodules
	@cd tomboy-library/ && git submodule init && git submodule update && cd ..
	
	xbuild Rainy.sln
	
