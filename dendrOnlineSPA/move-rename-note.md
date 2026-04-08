This document describes a new feature that will allow to rename a note underlying file name.
This will allow to move a note across the tree, or rename it.

# in tree view

a new icon will be added to each note in the tree view, that will allow to move or rename the note.

when clicked, it will open a dialog with the current note name and an autocomplte input field that will allow to select a new note name.
The dialog is essentially the same than the one used for promoting a stash note to a tree note.

 When the user selects a new note name and clicks ok, the note will be renamed to the new note name. 

Then the tree will be reloaded to reflect the changes.  

The move action will also be available in the command palette.  In this case the command will be `>move [note name]` and it will open the same dialog than the one used for promoting a stash note to a tree note.  


# in edit and view mode

The move action will be available in the command palette.  In this case the command will be `>move` and it will open the same dialog than the one used for promoting a stash note to a tree note.  
