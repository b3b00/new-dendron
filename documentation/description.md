
# Project description

Dendronline is a note taking system. 
It follow the [dendron](http://www.dendron.so) philosophy : define a not hierarchy based
on file naming convention

# technical stack

## back end
back end is a classical asp.net app.
It provides end points to access notes

Notes can be stored :
 - from local filesystem (for debug purpose)
 - from a github repo (using a OAuth code base authentication)

## front end

Front end is a svelte SPA.
It presents note on a hierarchy view and allow create/delete/modify notes. 
