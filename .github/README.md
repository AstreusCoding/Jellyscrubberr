# Jellyscrubberr

## Description

Alternative plugin for smooth video scrobbling in Jellyfin. This plugin generates trickplay files for videos in your library. Trickplay files are used to generate thumbnails and allow for smooth seeking in videos.

## Installation

NOTE: The client script will fail to inject automatically into the jellyfin-web server if there is a difference in permission between the owner of the web files (root, or www-data, etc.) and the executor of the main jellyfin-server. This often happens because...

* Docker - the container is being run as a non-root user while having been built as a root user, causing the web files to be owned by root. To solve this, you can remove any lines like `User: 1000:1000`, `GUID:`, `PID:`, etc. from the jellyfin docker compose file.

* Install from distro repositories - the jellyfin-server will execute as `jellyfin` user while the web files will be owned by `root`, `www-data`, etc. This can likely be fixed by adding the `jellyfin` (or whichever user your main jellyfin server runs at) to the same group the jellyfin-web folders are owned by. You should only do this if they are owned by a group other than root, and will have to lookup how to manage permissions on your specific distro.

* Alternatively, the script can manually be added to the index.html as described below.

NOTE: If you manually injected the script tag, you will have to manually inject it on every jellyfin-web update, as the index.html file will get overwritten. However, for normal Jellyscrubberr updates the script tag will not need to be changed as the plugin will return the latest script from /ClientScript

1. Add <https://raw.githubusercontent.com/CasperDoesCoding/jellyscrubberr/main/manifest.json> as a Jellyfin plugin repository
2. Install Jellyscrubberr from the repository
3. Restart the Jellyfin server
4. If your Jellyfin's web path is set, the plugin should automatically inject the companion client script into the "index.html" file of the web server directory. Otherwise, the line `<script plugin="Jellyscrubberr" version="1.0.0.0" src="/Trickplay/ClientScript"></script>` will have to be added at the end of the body tag manually right before `</body>`. If you have a base path set, change `src="/Trickplay/ClientScript"` to `src="/YOUR_BASE_PATH/Trickplay/ClientScript"`.
5. Clear your site cookies / local storage to get rid of the cached index file and receive a new one from the server.
6. Change any configuration options, like whether to save in media folders over internal metadata folders.
7. Run a scan (could take much longer depending on library size) or start watching a movie and the scrubbing preview should update in a few minutes.

## Todo

I intend to rewrite the creation and management of files but I am learning c# as of now so its a bit slow, but everything pushed to the main branch will always be functional.
If I make any major changes it will be released onto an development branch.

* [ ] Filter for which libraries / library type to generate trickplay files for.
* [ ] Smart on demand generation, start generating trickplay files for a video when it is played. If you watch x% of the video, generate a trickplay file for the episode after the one you are watching.
* [ ] Upon updating where to store the trickplay files, move the old trickplay files to the new location.
* [ ] Look into issue with not generating images with correct aspect ratio because aspect ratio is not based on DAR but rather the image resolution itself. (See [issue](https://github.com/nicknsy/jellyscrub/issues/128))
* [ ] Implement [Richy1989](https://github.com/Richy1989) multiple parallel video processing code from [pull request](https://github.com/nicknsy/jellyscrub/pull/144)

## Contributors

Project was forked from [nicknsy/jellyscrub](https://github.com/nicknsy/jellyscrub)

qscale option code taken from [timkrf](https://github.com/timkrf): [pull request](https://github.com/nicknsy/jellyscrub/pull/56)
