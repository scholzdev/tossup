#!/bin/sh
S=/private/tmp/claude-501/-Users-scholzf-dev-coin-flipper/347434b1-4061-44b6-9742-5a0ad0d3c6ee/scratchpad/r4
cd $S
i=0
for spec in "kb 1 0 0.45" "pad 2 1 0.45" "mouse 1 2 0.5" "kb 3 0 0.4" "pad 1 3 0.5" "mouse 2 0 0.45"; do
  set -- $spec
  i=$((i+1))
  MODE=$1 CHAR=$2 STAKE=$3 EASY=$4 REACH= DTMULT=4 PROFILE=$S/prof_seed.lua ./run.sh batch$i fresh
done
echo ALLDONE > $S/batch_done.txt
