# MIFTY (Mine Is Faster Than Yours)
## Mifty, it's really.. nifty!

It crossed my mind that after more than 25 years of programming various protocols and distributed systems, i've never actually developed a DNS client/server/resolver/forwarder. I'm bored and figured, why not?!

DNS is at the root of service discovery and without it we would have no internet, distributed computing or service meshes.

Currently I have a (maybe not fully) working DNS server and forwarder. This could be used as the basis for a full blown DNS server, or a kind of DNS proxy that could bridge networks and act as a gateway. There is also no reason why it couldn't be extended to include dynamic updates like for service discovery in systems like k8s or service mesh.

I am also using it as a sinkhole for bad DNS. I downloaded the dns blacklist from here: https://github.com/notracking/hosts-blocklists/ - and modified it a bit to suit my needs (mainly reversing the DNS order for top-down processing). Due to the volume of entries, for speed of processing, I also implemented a basic B-tree to store the structure.

---

I have also started to add command line options to avoid hard coding configurations etc.