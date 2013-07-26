If you are interested in contributing to the core of Pinta, like to fix
a bug or tweak existing code, you'll want to make your changes available
as pull requests on GitHub. Using Git is an important part of
contributing to Pinta, so if you want to read up on this we recommend
reading the excellent documentation on GitHub, or if you want an even
deeper understanding read [Pro Git](http://progit.org/book/).


####Claiming a bug to work on

If you plan to work on a bug that has been registered on our [Launchpad
page](https://bugs.launchpad.net/pinta/+bugs), then go to the relevant
bug and make sure no one else is already working on it, then leave
comment saying that you are working on it.

####Creating a GitHub Fork of Pinta

First you need  to make yourself an account with GitHub if you haven't
already got one. It is recommended that you use your full name, because
we need something to put in the credits! Then you fork the [PintaProject
Repository](https://github.com/PintaProject/Pinta) , and pull the code
down to your local machine. (All explained [here]
(http://help.github.com/fork-a-repo/))

####Writing, Compiling, and Testing Code

This is where the magic happens! Make whatever changes are necessary to
the source code, but please do not attempt to clean up existing code. We
are only interested in seeing functional changes to the code, if they
are mixed with cosmetic changes it becomes very difficult to see what
has actually changed. Build your code according to the [building
instructions](https://github.com/PintaProject/Pinta/blob/master/readme.md),
and test it to make sure it does what it's supposed to and doesn't do
anything it's not supposed to.

####Committing Your Modification to GitHub

When you are satisfied that you have a great fix to share with the
world, commit it to your GitHub fork. (See the second part of
[this page](http://help.github.com/create-a-repo/).) Make sure that the commit
message includes the bug number and a short description, this is vital
so that the devlopers know what exactly they are looking at. Then it
will be publicly visible for scrutiny, which comes in handy for the next
step.

####Sending a GitHub Pull Request

Pull requests are thoroughly explained [here](http://help.github.com/send-pull-requests/). You will want to send a
pull request to the Pinta repository from your fork. When the developers
see your request, they will test your solution and if it is good they
will merge it into the Pinta source code. They will then close the bug
on Launchpad and add your name to the credits as a contributor, and you
get a lovely warm feeling inside from helping make the world a better
place!


Questions on this proccess can be asked (as always) through the methods
described in `readme.md`
