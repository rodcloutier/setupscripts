#! /bin/bash

cd sample_scripts

rm -f ../repo/sample_scripts.1.0.zip
rm -f ../repo/sem_ver_sample_scripts.1.0.0.zip
rm -f ../repo/free_from_version_sample_scripts.tag.zip

zip ../repo/sample_scripts.1.0.zip sample.sh
zip ../repo/sem_ver_sample_scripts.1.0.0.zip sample.sh
zip ../repo/free_from_version_sample_scripts.tag.zip sample.sh

cd -
