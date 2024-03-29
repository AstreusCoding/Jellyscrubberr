# Jellyscrubberr

## Description

Allows smooth video scrobbling in Jellyfin.

## Todo

- [ ] Filter for which libraries / library type to generate trickplay files for.
- [ ] Upon generation of trickplay files check previous manifest file and compare the configuration to see if we need to regenerate the trickplay file or not.
- [ ] Smart on demand generation, start generating trickplay files for a video when it is played. If you watch x% of the video, generate a trickplay file for the episode after the one you are watching.
- [ ] Upon updating where to store the trickplay files, move the old trickplay files to the new location.
- [ ] Implement [Richy1989](https://github.com/Richy1989) multiple parallel video processing code from [pull request](https://github.com/nicknsy/jellyscrub/pull/144)
- [ ] Write installation instructions for Jellyfin.
- [ ] Look into issue with not generating images with correct aspect ratio because aspect ratio is not based on DAR but rather the image resolution itself. (See [issue](https://github.com/nicknsy/jellyscrub/issues/128))

## Original authors

Project was forked from [nicknsy/jellyscrub](https://github.com/nicknsy/jellyscrub)

qscale option code taken from [timkrf](https://github.com/timkrf): [pull request](https://github.com/nicknsy/jellyscrub/pull/56)
