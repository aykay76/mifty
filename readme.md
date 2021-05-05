# MIFTY (Mine Is Faster Than Yours)
## It's really... nifty...

It crossed my mind that after 27 years (on and off) of programming various protocols and distributed systems, i've never actually developed a DNS client/server/resolver/forwarder. I'm bored and figured, why not?!

DNS is at the root of everything and without it we would have no internet as you know it, so it's good to understand how it works. Plus I saw a thing about "faster than light" DNS and was triggered - can I make the fastest DNS "thing" but also make it cross platform?

And so another sideline project was born...

---

I have the basic understanding of how DNS works, so the question now is how this project will evolve. I could make a sinkhole kind of project where I eliminate the naughties, or turn this into what DNS has always been; a distributed key/value database. Or maybe it will become something else entirely, or just become another unfinished project :)

---

The basics are there - I essentially have a UDP tunnel at the moment that has a little specialist knowledge about DNS. This could easily be extended to do anything UDP can do, it could be a gateway between networks, a proxy, inspection agent, a server, a client. By adding multicast it could be the basis of a clustering protocol.

I've decided to work on my own version of an ad-blocker for now as it presents some interesting challenges - i'm using the `dnscrypt-proxy.blacklist.txt` from the kind people [here](https://github.com/notracking/hosts-blocklists). Now to make looking up from a list of 280k entries as efficient as humanly possible. I'm starting with some simple indexing but interested to learn more about Bloom filters, balanced trees and other data structures that might make this more efficient.
