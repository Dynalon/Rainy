#!/bin/bash

# fail on first error

set -e
# don't do anything on pull requests
if [ "$TRAVIS_PULL_REQUEST" != "false" ]; then
    exit 0
fi

git config --global user.email "travis@travis-ci.org"
git config --global user.name "TravisCI"

echo -e "Updating Rainy website with latest nightly build"

git clone --quiet --branch=gh-pages https://${GH_TOKEN}@github.com/Dynalon/Rainy.git $HOME/rainy-wiki > /dev/null
cp rainy-nightly.zip $HOME/rainy-wiki/nightly/
cd $HOME/rainy-wiki
git add nightly/rainy-nightly.zip

# add, commit and push files
git commit -m "Travis build $TRAVIS_BUILD_NUMBER pushed to gh-pages"
git push -fq origin gh-pages > /dev/null

echo -e "Done uploading latest nightly build"
