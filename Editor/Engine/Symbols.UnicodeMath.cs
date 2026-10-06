using System.Globalization;

namespace MathMesher.Editor.Engine
{
    /// <summary>
    /// Symbol commands generated from unicode-math-table.tex of the unicode-math package (LPPL 1.3c, see ThirdPartyNotices.md),
    /// limited to symbols whose glyphs exist in the bundled fonts. Entries never replace a command already defined by the
    /// classic LaTeX / AMS tables. Regenerate with Tools~/GenerateUnicodeMathSymbols.py.
    /// Format: "name hex class": o ord, b bin, r rel, O open, C close, p punct, L large operator, I integral,
    /// A accent, W wide accent, U bottom accent, V wide bottom accent, X over brace/arrow, Y under brace/arrow.
    /// </summary>
    internal static partial class Symbols
    {
        private const string UnicodeMathTable = @"
mathexclam 21 C|mathoctothorpe 23 o|mathdollar 24 o|mathpercent 25 o|mathampersand 26 o|lparen 28 O|rparen 29 C|mathplus 2B b|
mathcomma 2C p|mathperiod 2E o|mathslash 2F o|mathcolon 3A p|mathsemicolon 3B p|less 3C r|equal 3D r|greater 3E r|
mathquestion 3F o|mathatsign 40 o|lbrack 5B O|backslash 5C o|rbrack 5D C|lbrace 7B O|vert 7C o|rbrace 7D C|mathsterling A3 o|
mathyen A5 o|mathsection A7 o|neg AC o|pm B1 b|mathparagraph B6 o|cdotp B7 b|times D7 b|matheth F0 o|div F7 b|Zbar 1B5 o|
grave 300 A|acute 301 A|hat 302 A|widehat 302 W|tilde 303 A|widetilde 303 W|bar 304 A|overbar 305 A|wideoverbar 305 W|breve 306 A|
widebreve 306 W|dot 307 A|ddot 308 A|ovhook 309 A|ocirc 30A A|check 30C A|widecheck 30C W|candra 310 A|oturnedcomma 312 A|
ocommatopright 315 A|droang 31A A|wideutilde 330 V|mathunderbar 332 V|underleftrightarrow 34D V|mupAlpha 391 o|mupBeta 392 o|
mupGamma 393 o|mupDelta 394 o|mupEpsilon 395 o|mupZeta 396 o|mupEta 397 o|mupTheta 398 o|mupIota 399 o|mupKappa 39A o|
mupLambda 39B o|mupMu 39C o|mupNu 39D o|mupXi 39E o|mupOmicron 39F o|mupPi 3A0 o|mupRho 3A1 o|mupSigma 3A3 o|mupTau 3A4 o|
mupUpsilon 3A5 o|mupPhi 3A6 o|mupChi 3A7 o|mupPsi 3A8 o|mupOmega 3A9 o|mupalpha 3B1 o|mupbeta 3B2 o|mupgamma 3B3 o|mupdelta 3B4 o|
mupvarepsilon 3B5 o|mupzeta 3B6 o|mupeta 3B7 o|muptheta 3B8 o|mupiota 3B9 o|mupkappa 3BA o|muplambda 3BB o|mupmu 3BC o|mupnu 3BD o|
mupxi 3BE o|mupomicron 3BF o|muppi 3C0 o|muprho 3C1 o|mupvarsigma 3C2 o|mupsigma 3C3 o|muptau 3C4 o|mupupsilon 3C5 o|
mupvarphi 3C6 o|mupchi 3C7 o|muppsi 3C8 o|mupomega 3C9 o|mupvartheta 3D1 o|mupphi 3D5 o|mupvarpi 3D6 o|upDigamma 3DC o|
updigamma 3DD o|mupvarkappa 3F0 o|mupvarrho 3F1 o|mupvarTheta 3F4 o|mupepsilon 3F5 o|upbackepsilon 3F6 o|mathhyphen 2010 o|
horizbar 2015 o|Vert 2016 o|twolowline 2017 o|dagger 2020 b|ddagger 2021 b|smblkcircle 2022 b|enleadertwodots 2025 o|
unicodeellipsis 2026 o|prime 2032 o|dprime 2033 o|trprime 2034 o|backprime 2035 o|backdprime 2036 o|backtrprime 2037 o|
caretinsert 2038 o|Exclam 203C o|tieconcat 2040 b|hyphenbullet 2043 o|fracslash 2044 b|Question 2047 o|closure 2050 r|
qprime 2057 o|euro 20AC o|leftharpoonaccent 20D0 A|overleftharpoon 20D0 W|rightharpoonaccent 20D1 A|overrightharpoon 20D1 W|
vertoverlay 20D2 A|overleftarrow 20D6 W|overrightarrow 20D7 W|vec 20D7 A|dddot 20DB A|ddddot 20DC A|enclosecircle 20DD o|
enclosesquare 20DE o|enclosediamond 20DF o|overleftrightarrow 20E1 W|enclosetriangle 20E4 o|annuity 20E7 A|threeunderdot 20E8 U|
widebridgeabove 20E9 A|underrightharpoondown 20EC V|underleftharpoondown 20ED V|underleftarrow 20EE V|underrightarrow 20EF V|
asteraccent 20F0 A|BbbC 2102 o|Eulerconst 2107 o|mscrg 210A o|mscrH 210B o|mfrakH 210C o|BbbH 210D o|Planckconst 210E o|
hslash 210F o|mscrI 2110 o|Im 2111 o|mscrL 2112 o|ell 2113 o|BbbN 2115 o|wp 2118 o|BbbP 2119 o|BbbQ 211A o|mscrR 211B o|Re 211C o|
BbbR 211D o|BbbZ 2124 o|mho 2127 o|mfrakZ 2128 o|turnediota 2129 o|Angstrom 212B o|mscrB 212C o|mfrakC 212D o|mscre 212F o|
mscrE 2130 o|mscrF 2131 o|Finv 2132 o|mscrM 2133 o|mscro 2134 o|aleph 2135 o|beth 2136 o|gimel 2137 o|daleth 2138 o|Bbbpi 213C o|
Bbbgamma 213D o|BbbGamma 213E o|BbbPi 213F o|Bbbsum 2140 L|Game 2141 o|sansLturned 2142 o|sansLmirrored 2143 o|Yup 2144 o|
mitBbbD 2145 o|mitBbbd 2146 o|mitBbbe 2147 o|mitBbbi 2148 o|mitBbbj 2149 o|PropertyLine 214A o|upand 214B b|leftarrow 2190 r|
uparrow 2191 r|rightarrow 2192 r|downarrow 2193 r|leftrightarrow 2194 r|updownarrow 2195 r|nwarrow 2196 r|nearrow 2197 r|
searrow 2198 r|swarrow 2199 r|nleftarrow 219A r|nrightarrow 219B r|leftwavearrow 219C r|rightwavearrow 219D r|
twoheadleftarrow 219E r|twoheaduparrow 219F r|twoheadrightarrow 21A0 r|twoheaddownarrow 21A1 r|leftarrowtail 21A2 r|
rightarrowtail 21A3 r|mapsfrom 21A4 r|mapsup 21A5 r|mapsto 21A6 r|mapsdown 21A7 r|updownarrowbar 21A8 o|hookleftarrow 21A9 r|
hookrightarrow 21AA r|looparrowleft 21AB r|looparrowright 21AC r|leftrightsquigarrow 21AD r|nleftrightarrow 21AE r|
downzigzagarrow 21AF r|Lsh 21B0 r|Rsh 21B1 r|Ldsh 21B2 r|Rdsh 21B3 r|linefeed 21B4 o|carriagereturn 21B5 o|curvearrowleft 21B6 r|
curvearrowright 21B7 r|barovernorthwestarrow 21B8 o|barleftarrowrightarrowbar 21B9 o|acwopencirclearrow 21BA o|
cwopencirclearrow 21BB o|leftharpoonup 21BC r|leftharpoondown 21BD r|upharpoonright 21BE r|upharpoonleft 21BF r|
rightharpoonup 21C0 r|rightharpoondown 21C1 r|downharpoonright 21C2 r|downharpoonleft 21C3 r|rightleftarrows 21C4 r|
updownarrows 21C5 r|leftrightarrows 21C6 r|leftleftarrows 21C7 r|upuparrows 21C8 r|rightrightarrows 21C9 r|downdownarrows 21CA r|
leftrightharpoons 21CB r|rightleftharpoons 21CC r|nLeftarrow 21CD r|nLeftrightarrow 21CE r|nRightarrow 21CF r|Leftarrow 21D0 r|
Uparrow 21D1 r|Rightarrow 21D2 r|Downarrow 21D3 r|Leftrightarrow 21D4 r|Updownarrow 21D5 r|Nwarrow 21D6 r|Nearrow 21D7 r|
Searrow 21D8 r|Swarrow 21D9 r|Lleftarrow 21DA r|Rrightarrow 21DB r|leftsquigarrow 21DC r|rightsquigarrow 21DD r|nHuparrow 21DE o|
nHdownarrow 21DF o|leftdasharrow 21E0 o|updasharrow 21E1 o|rightdasharrow 21E2 o|downdasharrow 21E3 o|barleftarrow 21E4 r|
rightarrowbar 21E5 r|leftwhitearrow 21E6 o|upwhitearrow 21E7 o|rightwhitearrow 21E8 o|downwhitearrow 21E9 o|
whitearrowupfrombar 21EA o|circleonrightarrow 21F4 r|downuparrows 21F5 r|rightthreearrows 21F6 r|nvleftarrow 21F7 r|
nvrightarrow 21F8 r|nvleftrightarrow 21F9 r|nVleftarrow 21FA r|nVrightarrow 21FB r|nVleftrightarrow 21FC r|
leftarrowtriangle 21FD r|rightarrowtriangle 21FE r|leftrightarrowtriangle 21FF r|forall 2200 o|complement 2201 o|partial 2202 o|
exists 2203 o|nexists 2204 o|varnothing 2205 o|increment 2206 o|nabla 2207 o|in 2208 r|notin 2209 r|smallin 220A r|ni 220B r|
nni 220C r|smallni 220D r|QED 220E o|prod 220F L|coprod 2210 L|sum 2211 L|minus 2212 b|mp 2213 b|dotplus 2214 b|divslash 2215 b|
setminus 2216 b|ast 2217 b|vysmwhtcircle 2218 b|vysmblkcircle 2219 b|sqrt 221A O|surd 221A o|cuberoot 221B O|fourthroot 221C O|
propto 221D r|infty 221E o|rightangle 221F o|angle 2220 o|measuredangle 2221 o|sphericalangle 2222 o|mid 2223 r|nmid 2224 r|
parallel 2225 r|nparallel 2226 r|wedge 2227 b|vee 2228 b|cap 2229 b|cup 222A b|int 222B I|iint 222C I|iiint 222D I|oint 222E I|
oiint 222F I|oiiint 2230 I|intclockwise 2231 I|varointclockwise 2232 I|ointctrclockwise 2233 I|therefore 2234 o|because 2235 o|
mathratio 2236 r|Colon 2237 r|dotminus 2238 b|dashcolon 2239 r|dotsminusdots 223A r|kernelcontraction 223B r|sim 223C r|
backsim 223D r|invlazys 223E b|sinewave 223F o|wr 2240 b|nsim 2241 r|eqsim 2242 r|simeq 2243 r|nsime 2244 r|sime 2243 r|
nsimeq 2244 r|cong 2245 r|simneqq 2246 r|ncong 2247 r|approx 2248 r|napprox 2249 r|approxeq 224A r|approxident 224B r|
backcong 224C r|asymp 224D r|Bumpeq 224E r|bumpeq 224F r|doteq 2250 r|Doteq 2251 r|fallingdotseq 2252 r|risingdotseq 2253 r|
coloneq 2254 r|eqcolon 2255 r|eqcirc 2256 r|circeq 2257 r|arceq 2258 r|wedgeq 2259 r|veeeq 225A r|stareq 225B r|triangleq 225C r|
eqdef 225D r|measeq 225E r|questeq 225F r|ne 2260 r|equiv 2261 r|nequiv 2262 r|Equiv 2263 r|leq 2264 r|geq 2265 r|leqq 2266 r|
geqq 2267 r|lneqq 2268 r|gneqq 2269 r|ll 226A r|gg 226B r|between 226C r|nasymp 226D r|nless 226E r|ngtr 226F r|nleq 2270 r|
ngeq 2271 r|lesssim 2272 r|gtrsim 2273 r|nlesssim 2274 r|ngtrsim 2275 r|lessgtr 2276 r|gtrless 2277 r|nlessgtr 2278 r|
ngtrless 2279 r|prec 227A r|succ 227B r|preccurlyeq 227C r|succcurlyeq 227D r|precsim 227E r|succsim 227F r|nprec 2280 r|
nsucc 2281 r|subset 2282 r|supset 2283 r|nsubset 2284 r|nsupset 2285 r|subseteq 2286 r|supseteq 2287 r|nsubseteq 2288 r|
nsupseteq 2289 r|subsetneq 228A r|supsetneq 228B r|cupleftarrow 228C b|cupdot 228D b|uplus 228E b|sqsubset 228F r|sqsupset 2290 r|
sqsubseteq 2291 r|sqsupseteq 2292 r|sqcap 2293 b|sqcup 2294 b|oplus 2295 b|ominus 2296 b|otimes 2297 b|oslash 2298 b|odot 2299 b|
circledcirc 229A b|circledast 229B b|circledequal 229C b|circleddash 229D b|boxplus 229E b|boxminus 229F b|boxtimes 22A0 b|
boxdot 22A1 b|vdash 22A2 r|dashv 22A3 r|top 22A4 o|bot 22A5 o|assert 22A6 r|models 22A7 r|vDash 22A8 r|Vdash 22A9 r|Vvdash 22AA r|
VDash 22AB r|nvdash 22AC r|nvDash 22AD r|nVdash 22AE r|nVDash 22AF r|prurel 22B0 r|scurel 22B1 r|vartriangleleft 22B2 r|
vartriangleright 22B3 r|trianglelefteq 22B4 r|trianglerighteq 22B5 r|origof 22B6 r|imageof 22B7 r|multimap 22B8 r|
hermitmatrix 22B9 o|intercal 22BA b|veebar 22BB b|barwedge 22BC b|barvee 22BD b|measuredrightangle 22BE o|varlrtriangle 22BF o|
bigwedge 22C0 L|bigvee 22C1 L|bigcap 22C2 L|bigcup 22C3 L|smwhtdiamond 22C4 b|cdot 22C5 b|star 22C6 b|divideontimes 22C7 b|
bowtie 22C8 r|ltimes 22C9 b|rtimes 22CA b|leftthreetimes 22CB b|rightthreetimes 22CC b|backsimeq 22CD r|curlyvee 22CE b|
curlywedge 22CF b|Subset 22D0 r|Supset 22D1 r|Cap 22D2 b|Cup 22D3 b|pitchfork 22D4 r|equalparallel 22D5 r|lessdot 22D6 r|
gtrdot 22D7 r|lll 22D8 r|ggg 22D9 r|lesseqgtr 22DA r|gtreqless 22DB r|eqless 22DC r|eqgtr 22DD r|curlyeqprec 22DE r|
curlyeqsucc 22DF r|npreccurlyeq 22E0 r|nsucccurlyeq 22E1 r|nsqsubseteq 22E2 r|nsqsupseteq 22E3 r|sqsubsetneq 22E4 r|
sqsupsetneq 22E5 r|lnsim 22E6 r|gnsim 22E7 r|precnsim 22E8 r|succnsim 22E9 r|nvartriangleleft 22EA r|nvartriangleright 22EB r|
ntrianglelefteq 22EC r|ntrianglerighteq 22ED r|vdots 22EE r|unicodecdots 22EF o|adots 22F0 r|ddots 22F1 r|disin 22F2 r|
varisins 22F3 r|isins 22F4 r|isindot 22F5 r|varisinobar 22F6 r|isinobar 22F7 r|isinvb 22F8 r|isinE 22F9 r|nisd 22FA r|
varnis 22FB r|nis 22FC r|varniobar 22FD r|niobar 22FE r|bagmember 22FF r|diameter 2300 o|house 2302 o|varbarwedge 2305 b|
vardoublebarwedge 2306 b|lceil 2308 O|rceil 2309 C|lfloor 230A O|rfloor 230B C|invnot 2310 o|sqlozenge 2311 o|profline 2312 o|
profsurf 2313 o|viewdata 2317 o|turnednot 2319 o|ulcorner 231C O|urcorner 231D C|llcorner 231E O|lrcorner 231F C|inttop 2320 o|
intbottom 2321 o|frown 2322 r|smile 2323 r|varhexagonlrbonds 232C o|conictaper 2332 o|topbot 2336 o|obar 233D b|APLnotslash 233F r|
APLnotbackslash 2340 o|APLboxupcaret 2353 o|APLboxquestion 2370 o|rangledownzigzagarrow 237C o|hexagon 2394 o|lparenuend 239B o|
lparenextender 239C o|lparenlend 239D o|rparenuend 239E o|rparenextender 239F o|rparenlend 23A0 o|lbrackuend 23A1 o|
lbrackextender 23A2 o|lbracklend 23A3 o|rbrackuend 23A4 o|rbrackextender 23A5 o|rbracklend 23A6 o|lbraceuend 23A7 o|
lbracemid 23A8 o|lbracelend 23A9 o|vbraceextender 23AA o|rbraceuend 23AB o|rbracemid 23AC o|rbracelend 23AD o|intextender 23AE o|
harrowextender 23AF o|lmoustache 23B0 O|rmoustache 23B1 C|sumtop 23B2 o|sumbottom 23B3 o|overbracket 23B4 X|underbracket 23B5 Y|
bbrktbrk 23B6 o|sqrtbottom 23B7 o|lvboxline 23B8 o|rvboxline 23B9 o|varcarriagereturn 23CE o|overparen 23DC X|underparen 23DD Y|
overbrace 23DE X|underbrace 23DF Y|obrbrak 23E0 o|ubrbrak 23E1 o|trapezium 23E2 o|benzenr 23E3 o|strns 23E4 o|fltns 23E5 o|
accurrent 23E6 o|elinters 23E7 o|blanksymbol 2422 o|mathvisiblespace 2423 o|bdtriplevdash 2506 o|blockuphalf 2580 o|
blocklowhalf 2584 o|blockfull 2588 o|blocklefthalf 258C o|blockrighthalf 2590 o|blockqtrshaded 2591 o|blockhalfshaded 2592 o|
blockthreeqtrshaded 2593 o|mdlgblksquare 25A0 o|mdlgwhtsquare 25A1 o|squoval 25A2 o|blackinwhitesquare 25A3 o|squarehfill 25A4 o|
squarevfill 25A5 o|squarehvfill 25A6 o|squarenwsefill 25A7 o|squareneswfill 25A8 o|squarecrossfill 25A9 o|smblksquare 25AA o|
smwhtsquare 25AB o|hrectangleblack 25AC o|hrectangle 25AD o|vrectangleblack 25AE o|vrectangle 25AF o|parallelogramblack 25B0 o|
parallelogram 25B1 o|bigblacktriangleup 25B2 o|bigtriangleup 25B3 b|blacktriangle 25B4 o|vartriangle 25B5 r|
blacktriangleright 25B6 o|triangleright 25B7 b|smallblacktriangleright 25B8 o|smalltriangleright 25B9 o|blackpointerright 25BA o|
whitepointerright 25BB o|bigblacktriangledown 25BC o|bigtriangledown 25BD o|blacktriangledown 25BE o|triangledown 25BF o|
blacktriangleleft 25C0 o|triangleleft 25C1 b|smallblacktriangleleft 25C2 o|smalltriangleleft 25C3 o|blackpointerleft 25C4 o|
whitepointerleft 25C5 o|mdlgblkdiamond 25C6 o|mdlgwhtdiamond 25C7 o|blackinwhitediamond 25C8 o|fisheye 25C9 o|
mdlgwhtlozenge 25CA o|mdlgwhtcircle 25CB b|dottedcircle 25CC o|circlevertfill 25CD o|bullseye 25CE o|mdlgblkcircle 25CF o|
circlelefthalfblack 25D0 o|circlerighthalfblack 25D1 o|circlebottomhalfblack 25D2 o|circletophalfblack 25D3 o|
circleurquadblack 25D4 o|blackcircleulquadwhite 25D5 o|blacklefthalfcircle 25D6 o|blackrighthalfcircle 25D7 o|inversebullet 25D8 o|
inversewhitecircle 25D9 o|invwhiteupperhalfcircle 25DA o|invwhitelowerhalfcircle 25DB o|ularc 25DC o|urarc 25DD o|lrarc 25DE o|
llarc 25DF o|topsemicircle 25E0 o|botsemicircle 25E1 o|lrblacktriangle 25E2 o|llblacktriangle 25E3 o|ulblacktriangle 25E4 o|
urblacktriangle 25E5 o|smwhtcircle 25E6 o|squareleftblack 25E7 o|squarerightblack 25E8 o|squareulblack 25E9 o|squarelrblack 25EA o|
boxbar 25EB b|trianglecdot 25EC o|triangleleftblack 25ED o|trianglerightblack 25EE o|lgwhtcircle 25EF o|squareulquad 25F0 o|
squarellquad 25F1 o|squarelrquad 25F2 o|squareurquad 25F3 o|circleulquad 25F4 o|circlellquad 25F5 o|circlelrquad 25F6 o|
circleurquad 25F7 o|ultriangle 25F8 o|urtriangle 25F9 o|lltriangle 25FA o|mdwhtsquare 25FB o|mdblksquare 25FC o|
mdsmwhtsquare 25FD o|mdsmblksquare 25FE o|lrtriangle 25FF o|bigstar 2605 o|bigwhitestar 2606 o|astrosun 2609 o|danger 2621 o|
blacksmiley 263B o|sun 263C o|rightmoon 263D o|leftmoon 263E o|female 2640 o|male 2642 o|spadesuit 2660 o|heartsuit 2661 o|
diamondsuit 2662 o|clubsuit 2663 o|varspadesuit 2664 o|varheartsuit 2665 o|vardiamondsuit 2666 o|varclubsuit 2667 o|
quarternote 2669 o|eighthnote 266A o|twonotes 266B o|flat 266D o|natural 266E o|sharp 266F o|acidfree 267E o|dicei 2680 o|
diceii 2681 o|diceiii 2682 o|diceiv 2683 o|dicev 2684 o|dicevi 2685 o|circledrightdot 2686 o|circledtwodots 2687 o|
blackcircledrightdot 2688 o|blackcircledtwodots 2689 o|Hermaphrodite 26A5 o|mdwhtcircle 26AA o|mdblkcircle 26AB o|
mdsmwhtcircle 26AC o|neuter 26B2 o|checkmark 2713 o|maltese 2720 o|circledstar 272A o|varstar 2736 o|dingasterisk 273D o|
lbrbrak 2772 O|rbrbrak 2773 C|draftingarrow 279B o|threedangle 27C0 o|whiteinwhitetriangle 27C1 o|perp 27C2 r|subsetcirc 27C3 o|
supsetcirc 27C4 o|lbag 27C5 O|rbag 27C6 C|veedot 27C7 b|bsolhsub 27C8 r|suphsol 27C9 r|diagup 27CB o|longdivision 27CC O|
diagdown 27CD o|diamondcdot 27D0 o|wedgedot 27D1 b|upin 27D2 r|pullback 27D3 r|pushout 27D4 r|leftouterjoin 27D5 L|
rightouterjoin 27D6 L|fullouterjoin 27D7 L|bigbot 27D8 L|bigtop 27D9 L|DashVDash 27DA r|dashVdash 27DB r|multimapinv 27DC r|
vlongdash 27DD r|longdashv 27DE r|cirbot 27DF r|lozengeminus 27E0 b|concavediamond 27E1 b|concavediamondtickleft 27E2 b|
concavediamondtickright 27E3 b|whitesquaretickleft 27E4 b|whitesquaretickright 27E5 b|lBrack 27E6 O|rBrack 27E7 C|langle 27E8 O|
rangle 27E9 C|lAngle 27EA O|rAngle 27EB C|Lbrbrak 27EC O|Rbrbrak 27ED C|lgroup 27EE O|rgroup 27EF C|UUparrow 27F0 r|
DDownarrow 27F1 r|acwgapcirclearrow 27F2 r|cwgapcirclearrow 27F3 r|rightarrowonoplus 27F4 r|longleftarrow 27F5 r|
longrightarrow 27F6 r|longleftrightarrow 27F7 r|Longleftarrow 27F8 r|Longrightarrow 27F9 r|Longleftrightarrow 27FA r|
longmapsfrom 27FB r|longmapsto 27FC r|Longmapsfrom 27FD r|Longmapsto 27FE r|longrightsquigarrow 27FF r|nvtwoheadrightarrow 2900 r|
nVtwoheadrightarrow 2901 r|nvLeftarrow 2902 r|nvRightarrow 2903 r|nvLeftrightarrow 2904 r|twoheadmapsto 2905 r|Mapsfrom 2906 r|
Mapsto 2907 r|downarrowbarred 2908 r|uparrowbarred 2909 r|Uuparrow 290A r|Ddownarrow 290B r|leftbkarrow 290C r|rightbkarrow 290D r|
leftdbkarrow 290E r|dbkarrow 290F r|drbkarrow 2910 r|rightdotarrow 2911 r|baruparrow 2912 r|downarrowbar 2913 r|
nvrightarrowtail 2914 r|nVrightarrowtail 2915 r|twoheadrightarrowtail 2916 r|nvtwoheadrightarrowtail 2917 r|
nVtwoheadrightarrowtail 2918 r|lefttail 2919 r|righttail 291A r|leftdbltail 291B r|rightdbltail 291C r|diamondleftarrow 291D r|
rightarrowdiamond 291E r|diamondleftarrowbar 291F r|barrightarrowdiamond 2920 r|nwsearrow 2921 r|neswarrow 2922 r|hknwarrow 2923 r|
hknearrow 2924 r|hksearrow 2925 r|hkswarrow 2926 r|tona 2927 r|toea 2928 r|tosa 2929 r|towa 292A r|rdiagovfdiag 292B o|
fdiagovrdiag 292C o|seovnearrow 292D o|neovsearrow 292E o|fdiagovnearrow 292F o|rdiagovsearrow 2930 o|neovnwarrow 2931 o|
nwovnearrow 2932 o|rightcurvedarrow 2933 r|uprightcurvearrow 2934 o|downrightcurvedarrow 2935 o|leftdowncurvedarrow 2936 r|
rightdowncurvedarrow 2937 r|cwrightarcarrow 2938 r|acwleftarcarrow 2939 r|acwoverarcarrow 293A r|acwunderarcarrow 293B r|
curvearrowrightminus 293C r|curvearrowleftplus 293D r|cwundercurvearrow 293E r|ccwundercurvearrow 293F r|acwcirclearrow 2940 r|
cwcirclearrow 2941 r|rightarrowshortleftarrow 2942 r|leftarrowshortrightarrow 2943 r|shortrightarrowleftarrow 2944 r|
rightarrowplus 2945 r|leftarrowplus 2946 r|rightarrowx 2947 r|leftrightarrowcircle 2948 r|twoheaduparrowcircle 2949 r|
leftrightharpoonupdown 294A r|leftrightharpoondownup 294B r|updownharpoonrightleft 294C r|updownharpoonleftright 294D r|
leftrightharpoonupup 294E r|updownharpoonrightright 294F r|leftrightharpoondowndown 2950 r|updownharpoonleftleft 2951 r|
barleftharpoonup 2952 r|rightharpoonupbar 2953 r|barupharpoonright 2954 r|downharpoonrightbar 2955 r|barleftharpoondown 2956 r|
rightharpoondownbar 2957 r|barupharpoonleft 2958 r|downharpoonleftbar 2959 r|leftharpoonupbar 295A r|barrightharpoonup 295B r|
upharpoonrightbar 295C r|bardownharpoonright 295D r|leftharpoondownbar 295E r|barrightharpoondown 295F r|upharpoonleftbar 2960 r|
bardownharpoonleft 2961 r|leftharpoonsupdown 2962 r|upharpoonsleftright 2963 r|rightharpoonsupdown 2964 r|
downharpoonsleftright 2965 r|leftrightharpoonsup 2966 r|leftrightharpoonsdown 2967 r|rightleftharpoonsup 2968 r|
rightleftharpoonsdown 2969 r|leftharpoonupdash 296A r|dashleftharpoondown 296B r|rightharpoonupdash 296C r|
dashrightharpoondown 296D r|updownharpoonsleftright 296E r|downupharpoonsleftright 296F r|rightimply 2970 r|equalrightarrow 2971 r|
similarrightarrow 2972 r|leftarrowsimilar 2973 r|rightarrowsimilar 2974 r|rightarrowapprox 2975 r|ltlarr 2976 r|
leftarrowless 2977 r|gtrarr 2978 r|subrarr 2979 r|leftarrowsubset 297A r|suplarr 297B r|leftfishtail 297C r|rightfishtail 297D r|
upfishtail 297E r|downfishtail 297F r|Vvert 2980 o|mdsmblkcircle 2981 o|typecolon 2982 r|lBrace 2983 O|rBrace 2984 C|lParen 2985 O|
rParen 2986 C|llparenthesis 2987 O|rrparenthesis 2988 C|llangle 2989 O|rrangle 298A C|lbrackubar 298B O|rbrackubar 298C C|
lbrackultick 298D O|rbracklrtick 298E C|lbracklltick 298F O|rbrackurtick 2990 C|langledot 2991 O|rangledot 2992 C|
lparenless 2993 O|rparengtr 2994 C|Lparengtr 2995 O|Rparenless 2996 C|lblkbrbrak 2997 O|rblkbrbrak 2998 C|fourvdots 2999 o|
vzigzag 299A o|measuredangleleft 299B o|rightanglesqr 299C o|rightanglemdot 299D o|angles 299E o|angdnr 299F o|gtlpar 29A0 o|
sphericalangleup 29A1 o|turnangle 29A2 o|revangle 29A3 o|angleubar 29A4 o|revangleubar 29A5 o|wideangledown 29A6 o|
wideangleup 29A7 o|measanglerutone 29A8 o|measanglelutonw 29A9 o|measanglerdtose 29AA o|measangleldtosw 29AB o|
measangleurtone 29AC o|measangleultonw 29AD o|measangledrtose 29AE o|measangledltosw 29AF o|revemptyset 29B0 o|emptysetobar 29B1 o|
emptysetocirc 29B2 o|emptysetoarr 29B3 o|emptysetoarrl 29B4 o|circlehbar 29B5 b|circledvert 29B6 b|circledparallel 29B7 b|
obslash 29B8 b|operp 29B9 b|obot 29BA o|olcross 29BB o|odotslashdot 29BC o|uparrowoncircle 29BD o|circledwhitebullet 29BE o|
circledbullet 29BF o|olessthan 29C0 b|ogreaterthan 29C1 b|cirscir 29C2 o|cirE 29C3 o|boxdiag 29C4 b|boxbslash 29C5 b|boxast 29C6 b|
boxcircle 29C7 b|boxbox 29C8 b|boxonbox 29C9 o|triangleodot 29CA o|triangleubar 29CB o|triangles 29CC o|triangleserifs 29CD b|
rtriltri 29CE r|ltrivb 29CF r|vbrtri 29D0 r|lfbowtie 29D1 r|rfbowtie 29D2 r|fbowtie 29D3 r|lftimes 29D4 r|rftimes 29D5 r|
hourglass 29D6 b|blackhourglass 29D7 b|lvzigzag 29D8 O|rvzigzag 29D9 C|Lvzigzag 29DA O|Rvzigzag 29DB C|iinfin 29DC o|
tieinfty 29DD o|nvinfty 29DE o|dualmap 29DF r|laplac 29E0 o|lrtriangleeq 29E1 r|shuffle 29E2 b|eparsl 29E3 r|smeparsl 29E4 r|
eqvparsl 29E5 r|gleichstark 29E6 r|thermod 29E7 o|downtriangleleftblack 29E8 o|downtrianglerightblack 29E9 o|
blackdiamonddownarrow 29EA o|mdlgblklozenge 29EB b|circledownarrow 29EC o|blackcircledownarrow 29ED o|errbarsquare 29EE o|
errbarblacksquare 29EF o|errbardiamond 29F0 o|errbarblackdiamond 29F1 o|errbarcircle 29F2 o|errbarblackcircle 29F3 o|
ruledelayed 29F4 r|reversesolidus 29F5 b|dsol 29F6 b|rsolbar 29F7 b|xsol 29F8 L|xbsol 29F9 L|doubleplus 29FA b|tripleplus 29FB b|
lcurvyangle 29FC O|rcurvyangle 29FD C|tplus 29FE b|tminus 29FF b|bigodot 2A00 L|bigoplus 2A01 L|bigotimes 2A02 L|bigcupdot 2A03 L|
biguplus 2A04 L|bigsqcap 2A05 L|bigsqcup 2A06 L|conjquant 2A07 L|disjquant 2A08 L|bigtimes 2A09 L|modtwosum 2A0A L|sumint 2A0B I|
iiiint 2A0C I|intbar 2A0D I|intBar 2A0E I|fint 2A0F I|cirfnint 2A10 I|awint 2A11 I|rppolint 2A12 I|scpolint 2A13 I|npolint 2A14 I|
pointint 2A15 I|sqint 2A16 I|intlarhk 2A17 I|intx 2A18 I|intcap 2A19 I|intcup 2A1A I|upint 2A1B I|lowint 2A1C I|Join 2A1D L|
bigtriangleleft 2A1E L|zcmp 2A1F L|zpipe 2A20 L|zproject 2A21 L|ringplus 2A22 b|plushat 2A23 b|simplus 2A24 b|plusdot 2A25 b|
plussim 2A26 b|plussubtwo 2A27 b|plustrif 2A28 b|commaminus 2A29 b|minusdot 2A2A b|minusfdots 2A2B b|minusrdots 2A2C b|
opluslhrim 2A2D b|oplusrhrim 2A2E b|vectimes 2A2F b|dottimes 2A30 b|timesbar 2A31 b|btimes 2A32 b|smashtimes 2A33 b|
otimeslhrim 2A34 b|otimesrhrim 2A35 b|otimeshat 2A36 b|Otimes 2A37 b|odiv 2A38 b|triangleplus 2A39 b|triangleminus 2A3A b|
triangletimes 2A3B b|intprod 2A3C b|intprodr 2A3D b|fcmp 2A3E b|amalg 2A3F b|capdot 2A40 b|uminus 2A41 b|barcup 2A42 b|
barcap 2A43 b|capwedge 2A44 b|cupvee 2A45 b|cupovercap 2A46 b|capovercup 2A47 b|cupbarcap 2A48 b|capbarcup 2A49 b|twocups 2A4A b|
twocaps 2A4B b|closedvarcup 2A4C b|closedvarcap 2A4D b|Sqcap 2A4E b|Sqcup 2A4F b|closedvarcupsmashprod 2A50 b|wedgeodot 2A51 b|
veeodot 2A52 b|Wedge 2A53 b|Vee 2A54 b|wedgeonwedge 2A55 b|veeonvee 2A56 b|bigslopedvee 2A57 b|bigslopedwedge 2A58 b|
veeonwedge 2A59 r|wedgemidvert 2A5A b|veemidvert 2A5B b|midbarwedge 2A5C b|midbarvee 2A5D b|doublebarwedge 2A5E b|wedgebar 2A5F b|
wedgedoublebar 2A60 b|varveebar 2A61 b|doublebarvee 2A62 b|veedoublebar 2A63 b|dsub 2A64 b|rsub 2A65 b|eqdot 2A66 r|
dotequiv 2A67 r|equivVert 2A68 r|equivVvert 2A69 r|dotsim 2A6A r|simrdots 2A6B r|simminussim 2A6C r|congdot 2A6D r|asteq 2A6E r|
hatapprox 2A6F r|approxeqq 2A70 r|eqqplus 2A71 b|pluseqq 2A72 b|eqqsim 2A73 r|Coloneq 2A74 r|eqeq 2A75 r|eqeqeq 2A76 r|
ddotseq 2A77 r|equivDD 2A78 r|ltcir 2A79 r|gtcir 2A7A r|ltquest 2A7B r|gtquest 2A7C r|leqslant 2A7D r|geqslant 2A7E r|
lesdot 2A7F r|gesdot 2A80 r|lesdoto 2A81 r|gesdoto 2A82 r|lesdotor 2A83 r|gesdotol 2A84 r|lessapprox 2A85 r|gtrapprox 2A86 r|
lneq 2A87 r|gneq 2A88 r|lnapprox 2A89 r|gnapprox 2A8A r|lesseqqgtr 2A8B r|gtreqqless 2A8C r|lsime 2A8D r|gsime 2A8E r|lsimg 2A8F r|
gsiml 2A90 r|lgE 2A91 r|glE 2A92 r|lesges 2A93 r|gesles 2A94 r|eqslantless 2A95 r|eqslantgtr 2A96 r|elsdot 2A97 r|egsdot 2A98 r|
eqqless 2A99 r|eqqgtr 2A9A r|eqqslantless 2A9B r|eqqslantgtr 2A9C r|simless 2A9D r|simgtr 2A9E r|simlE 2A9F r|simgE 2AA0 r|
Lt 2AA1 r|Gt 2AA2 r|partialmeetcontraction 2AA3 r|glj 2AA4 r|gla 2AA5 r|ltcc 2AA6 r|gtcc 2AA7 r|lescc 2AA8 r|gescc 2AA9 r|
smt 2AAA r|lat 2AAB r|smte 2AAC r|late 2AAD r|bumpeqq 2AAE r|preceq 2AAF r|succeq 2AB0 r|precneq 2AB1 r|succneq 2AB2 r|
preceqq 2AB3 r|succeqq 2AB4 r|precneqq 2AB5 r|succneqq 2AB6 r|precapprox 2AB7 r|succapprox 2AB8 r|precnapprox 2AB9 r|
succnapprox 2ABA r|Prec 2ABB r|Succ 2ABC r|subsetdot 2ABD r|supsetdot 2ABE r|subsetplus 2ABF r|supsetplus 2AC0 r|submult 2AC1 r|
supmult 2AC2 r|subedot 2AC3 r|supedot 2AC4 r|subseteqq 2AC5 r|supseteqq 2AC6 r|subsim 2AC7 r|supsim 2AC8 r|subsetapprox 2AC9 r|
supsetapprox 2ACA r|subsetneqq 2ACB r|supsetneqq 2ACC r|lsqhook 2ACD r|rsqhook 2ACE r|csub 2ACF r|csup 2AD0 r|csube 2AD1 r|
csupe 2AD2 r|subsup 2AD3 r|supsub 2AD4 r|subsub 2AD5 r|supsup 2AD6 r|suphsub 2AD7 r|supdsub 2AD8 r|forkv 2AD9 r|topfork 2ADA r|
mlcp 2ADB r|forks 2ADC r|forksnot 2ADD r|shortlefttack 2ADE r|shortdowntack 2ADF r|shortuptack 2AE0 r|perps 2AE1 o|vDdash 2AE2 r|
dashV 2AE3 r|Dashv 2AE4 r|DashV 2AE5 r|varVdash 2AE6 r|Barv 2AE7 r|vBar 2AE8 r|vBarv 2AE9 r|barV 2AEA r|Vbar 2AEB r|Not 2AEC r|
bNot 2AED r|revnmid 2AEE r|cirmid 2AEF r|midcir 2AF0 r|topcir 2AF1 o|nhpar 2AF2 r|parsim 2AF3 r|interleave 2AF4 b|nhVvert 2AF5 b|
threedotcolon 2AF6 b|lllnest 2AF7 r|gggnest 2AF8 r|leqqslant 2AF9 r|geqqslant 2AFA r|trslash 2AFB b|biginterleave 2AFC I|
sslash 2AFD b|talloblong 2AFE b|bigtalloblong 2AFF L|squaretopblack 2B12 o|squarebotblack 2B13 o|squareurblack 2B14 o|
squarellblack 2B15 o|diamondleftblack 2B16 o|diamondrightblack 2B17 o|diamondtopblack 2B18 o|diamondbotblack 2B19 o|
dottedsquare 2B1A o|lgblksquare 2B1B o|lgwhtsquare 2B1C o|vysmblksquare 2B1D o|vysmwhtsquare 2B1E o|pentagonblack 2B1F o|
pentagon 2B20 o|varhexagon 2B21 o|varhexagonblack 2B22 o|hexagonblack 2B23 o|lgblkcircle 2B24 o|mdblkdiamond 2B25 o|
mdwhtdiamond 2B26 o|mdblklozenge 2B27 o|mdwhtlozenge 2B28 o|smblkdiamond 2B29 o|smblklozenge 2B2A o|smwhtlozenge 2B2B o|
blkhorzoval 2B2C o|whthorzoval 2B2D o|blkvertoval 2B2E o|whtvertoval 2B2F o|circleonleftarrow 2B30 r|leftthreearrows 2B31 r|
leftarrowonoplus 2B32 r|longleftsquigarrow 2B33 r|nvtwoheadleftarrow 2B34 r|nVtwoheadleftarrow 2B35 r|twoheadmapsfrom 2B36 r|
twoheadleftdbkarrow 2B37 r|leftdotarrow 2B38 r|nvleftarrowtail 2B39 r|nVleftarrowtail 2B3A r|twoheadleftarrowtail 2B3B r|
nvtwoheadleftarrowtail 2B3C r|nVtwoheadleftarrowtail 2B3D r|leftarrowx 2B3E r|leftcurvedarrow 2B3F r|equalleftarrow 2B40 r|
bsimilarleftarrow 2B41 r|leftarrowbackapprox 2B42 r|rightarrowgtr 2B43 r|rightarrowsupset 2B44 r|LLeftarrow 2B45 r|
RRightarrow 2B46 r|bsimilarrightarrow 2B47 r|rightarrowbackapprox 2B48 r|similarleftarrow 2B49 r|leftarrowapprox 2B4A r|
leftarrowbsimilar 2B4B r|rightarrowbsimilar 2B4C r|medwhitestar 2B50 o|medblackstar 2B51 o|smwhitestar 2B52 o|
rightpentagonblack 2B53 o|rightpentagon 2B54 o|postalmark 3012 o|hzigzag 3030 o|mbfA 1D400 o|mbfB 1D401 o|mbfC 1D402 o|
mbfD 1D403 o|mbfE 1D404 o|mbfF 1D405 o|mbfG 1D406 o|mbfH 1D407 o|mbfI 1D408 o|mbfJ 1D409 o|mbfK 1D40A o|mbfL 1D40B o|mbfM 1D40C o|
mbfN 1D40D o|mbfO 1D40E o|mbfP 1D40F o|mbfQ 1D410 o|mbfR 1D411 o|mbfS 1D412 o|mbfT 1D413 o|mbfU 1D414 o|mbfV 1D415 o|mbfW 1D416 o|
mbfX 1D417 o|mbfY 1D418 o|mbfZ 1D419 o|mbfa 1D41A o|mbfb 1D41B o|mbfc 1D41C o|mbfd 1D41D o|mbfe 1D41E o|mbff 1D41F o|mbfg 1D420 o|
mbfh 1D421 o|mbfi 1D422 o|mbfj 1D423 o|mbfk 1D424 o|mbfl 1D425 o|mbfm 1D426 o|mbfn 1D427 o|mbfo 1D428 o|mbfp 1D429 o|mbfq 1D42A o|
mbfr 1D42B o|mbfs 1D42C o|mbft 1D42D o|mbfu 1D42E o|mbfv 1D42F o|mbfw 1D430 o|mbfx 1D431 o|mbfy 1D432 o|mbfz 1D433 o|mitA 1D434 o|
mitB 1D435 o|mitC 1D436 o|mitD 1D437 o|mitE 1D438 o|mitF 1D439 o|mitG 1D43A o|mitH 1D43B o|mitI 1D43C o|mitJ 1D43D o|mitK 1D43E o|
mitL 1D43F o|mitM 1D440 o|mitN 1D441 o|mitO 1D442 o|mitP 1D443 o|mitQ 1D444 o|mitR 1D445 o|mitS 1D446 o|mitT 1D447 o|mitU 1D448 o|
mitV 1D449 o|mitW 1D44A o|mitX 1D44B o|mitY 1D44C o|mitZ 1D44D o|mita 1D44E o|mitb 1D44F o|mitc 1D450 o|mitd 1D451 o|mite 1D452 o|
mitf 1D453 o|mitg 1D454 o|miti 1D456 o|mitj 1D457 o|mitk 1D458 o|mitl 1D459 o|mitm 1D45A o|mitn 1D45B o|mito 1D45C o|mitp 1D45D o|
mitq 1D45E o|mitr 1D45F o|mits 1D460 o|mitt 1D461 o|mitu 1D462 o|mitv 1D463 o|mitw 1D464 o|mitx 1D465 o|mity 1D466 o|mitz 1D467 o|
mbfitA 1D468 o|mbfitB 1D469 o|mbfitC 1D46A o|mbfitD 1D46B o|mbfitE 1D46C o|mbfitF 1D46D o|mbfitG 1D46E o|mbfitH 1D46F o|
mbfitI 1D470 o|mbfitJ 1D471 o|mbfitK 1D472 o|mbfitL 1D473 o|mbfitM 1D474 o|mbfitN 1D475 o|mbfitO 1D476 o|mbfitP 1D477 o|
mbfitQ 1D478 o|mbfitR 1D479 o|mbfitS 1D47A o|mbfitT 1D47B o|mbfitU 1D47C o|mbfitV 1D47D o|mbfitW 1D47E o|mbfitX 1D47F o|
mbfitY 1D480 o|mbfitZ 1D481 o|mbfita 1D482 o|mbfitb 1D483 o|mbfitc 1D484 o|mbfitd 1D485 o|mbfite 1D486 o|mbfitf 1D487 o|
mbfitg 1D488 o|mbfith 1D489 o|mbfiti 1D48A o|mbfitj 1D48B o|mbfitk 1D48C o|mbfitl 1D48D o|mbfitm 1D48E o|mbfitn 1D48F o|
mbfito 1D490 o|mbfitp 1D491 o|mbfitq 1D492 o|mbfitr 1D493 o|mbfits 1D494 o|mbfitt 1D495 o|mbfitu 1D496 o|mbfitv 1D497 o|
mbfitw 1D498 o|mbfitx 1D499 o|mbfity 1D49A o|mbfitz 1D49B o|mscrA 1D49C o|mscrC 1D49E o|mscrD 1D49F o|mscrG 1D4A2 o|mscrJ 1D4A5 o|
mscrK 1D4A6 o|mscrN 1D4A9 o|mscrO 1D4AA o|mscrP 1D4AB o|mscrQ 1D4AC o|mscrS 1D4AE o|mscrT 1D4AF o|mscrU 1D4B0 o|mscrV 1D4B1 o|
mscrW 1D4B2 o|mscrX 1D4B3 o|mscrY 1D4B4 o|mscrZ 1D4B5 o|mscra 1D4B6 o|mscrb 1D4B7 o|mscrc 1D4B8 o|mscrd 1D4B9 o|mscrf 1D4BB o|
mscrh 1D4BD o|mscri 1D4BE o|mscrj 1D4BF o|mscrk 1D4C0 o|mscrl 1D4C1 o|mscrm 1D4C2 o|mscrn 1D4C3 o|mscrp 1D4C5 o|mscrq 1D4C6 o|
mscrr 1D4C7 o|mscrs 1D4C8 o|mscrt 1D4C9 o|mscru 1D4CA o|mscrv 1D4CB o|mscrw 1D4CC o|mscrx 1D4CD o|mscry 1D4CE o|mscrz 1D4CF o|
mbfscrA 1D4D0 o|mbfscrB 1D4D1 o|mbfscrC 1D4D2 o|mbfscrD 1D4D3 o|mbfscrE 1D4D4 o|mbfscrF 1D4D5 o|mbfscrG 1D4D6 o|mbfscrH 1D4D7 o|
mbfscrI 1D4D8 o|mbfscrJ 1D4D9 o|mbfscrK 1D4DA o|mbfscrL 1D4DB o|mbfscrM 1D4DC o|mbfscrN 1D4DD o|mbfscrO 1D4DE o|mbfscrP 1D4DF o|
mbfscrQ 1D4E0 o|mbfscrR 1D4E1 o|mbfscrS 1D4E2 o|mbfscrT 1D4E3 o|mbfscrU 1D4E4 o|mbfscrV 1D4E5 o|mbfscrW 1D4E6 o|mbfscrX 1D4E7 o|
mbfscrY 1D4E8 o|mbfscrZ 1D4E9 o|mbfscra 1D4EA o|mbfscrb 1D4EB o|mbfscrc 1D4EC o|mbfscrd 1D4ED o|mbfscre 1D4EE o|mbfscrf 1D4EF o|
mbfscrg 1D4F0 o|mbfscrh 1D4F1 o|mbfscri 1D4F2 o|mbfscrj 1D4F3 o|mbfscrk 1D4F4 o|mbfscrl 1D4F5 o|mbfscrm 1D4F6 o|mbfscrn 1D4F7 o|
mbfscro 1D4F8 o|mbfscrp 1D4F9 o|mbfscrq 1D4FA o|mbfscrr 1D4FB o|mbfscrs 1D4FC o|mbfscrt 1D4FD o|mbfscru 1D4FE o|mbfscrv 1D4FF o|
mbfscrw 1D500 o|mbfscrx 1D501 o|mbfscry 1D502 o|mbfscrz 1D503 o|mfrakA 1D504 o|mfrakB 1D505 o|mfrakD 1D507 o|mfrakE 1D508 o|
mfrakF 1D509 o|mfrakG 1D50A o|mfrakJ 1D50D o|mfrakK 1D50E o|mfrakL 1D50F o|mfrakM 1D510 o|mfrakN 1D511 o|mfrakO 1D512 o|
mfrakP 1D513 o|mfrakQ 1D514 o|mfrakS 1D516 o|mfrakT 1D517 o|mfrakU 1D518 o|mfrakV 1D519 o|mfrakW 1D51A o|mfrakX 1D51B o|
mfrakY 1D51C o|mfraka 1D51E o|mfrakb 1D51F o|mfrakc 1D520 o|mfrakd 1D521 o|mfrake 1D522 o|mfrakf 1D523 o|mfrakg 1D524 o|
mfrakh 1D525 o|mfraki 1D526 o|mfrakj 1D527 o|mfrakk 1D528 o|mfrakl 1D529 o|mfrakm 1D52A o|mfrakn 1D52B o|mfrako 1D52C o|
mfrakp 1D52D o|mfrakq 1D52E o|mfrakr 1D52F o|mfraks 1D530 o|mfrakt 1D531 o|mfraku 1D532 o|mfrakv 1D533 o|mfrakw 1D534 o|
mfrakx 1D535 o|mfraky 1D536 o|mfrakz 1D537 o|BbbA 1D538 o|BbbB 1D539 o|BbbD 1D53B o|BbbE 1D53C o|BbbF 1D53D o|BbbG 1D53E o|
BbbI 1D540 o|BbbJ 1D541 o|BbbK 1D542 o|BbbL 1D543 o|BbbM 1D544 o|BbbO 1D546 o|BbbS 1D54A o|BbbT 1D54B o|BbbU 1D54C o|BbbV 1D54D o|
BbbW 1D54E o|BbbX 1D54F o|BbbY 1D550 o|Bbba 1D552 o|Bbbb 1D553 o|Bbbc 1D554 o|Bbbd 1D555 o|Bbbe 1D556 o|Bbbf 1D557 o|Bbbg 1D558 o|
Bbbh 1D559 o|Bbbi 1D55A o|Bbbj 1D55B o|Bbbk 1D55C o|Bbbl 1D55D o|Bbbm 1D55E o|Bbbn 1D55F o|Bbbo 1D560 o|Bbbp 1D561 o|Bbbq 1D562 o|
Bbbr 1D563 o|Bbbs 1D564 o|Bbbt 1D565 o|Bbbu 1D566 o|Bbbv 1D567 o|Bbbw 1D568 o|Bbbx 1D569 o|Bbby 1D56A o|Bbbz 1D56B o|
mbffrakA 1D56C o|mbffrakB 1D56D o|mbffrakC 1D56E o|mbffrakD 1D56F o|mbffrakE 1D570 o|mbffrakF 1D571 o|mbffrakG 1D572 o|
mbffrakH 1D573 o|mbffrakI 1D574 o|mbffrakJ 1D575 o|mbffrakK 1D576 o|mbffrakL 1D577 o|mbffrakM 1D578 o|mbffrakN 1D579 o|
mbffrakO 1D57A o|mbffrakP 1D57B o|mbffrakQ 1D57C o|mbffrakR 1D57D o|mbffrakS 1D57E o|mbffrakT 1D57F o|mbffrakU 1D580 o|
mbffrakV 1D581 o|mbffrakW 1D582 o|mbffrakX 1D583 o|mbffrakY 1D584 o|mbffrakZ 1D585 o|mbffraka 1D586 o|mbffrakb 1D587 o|
mbffrakc 1D588 o|mbffrakd 1D589 o|mbffrake 1D58A o|mbffrakf 1D58B o|mbffrakg 1D58C o|mbffrakh 1D58D o|mbffraki 1D58E o|
mbffrakj 1D58F o|mbffrakk 1D590 o|mbffrakl 1D591 o|mbffrakm 1D592 o|mbffrakn 1D593 o|mbffrako 1D594 o|mbffrakp 1D595 o|
mbffrakq 1D596 o|mbffrakr 1D597 o|mbffraks 1D598 o|mbffrakt 1D599 o|mbffraku 1D59A o|mbffrakv 1D59B o|mbffrakw 1D59C o|
mbffrakx 1D59D o|mbffraky 1D59E o|mbffrakz 1D59F o|msansA 1D5A0 o|msansB 1D5A1 o|msansC 1D5A2 o|msansD 1D5A3 o|msansE 1D5A4 o|
msansF 1D5A5 o|msansG 1D5A6 o|msansH 1D5A7 o|msansI 1D5A8 o|msansJ 1D5A9 o|msansK 1D5AA o|msansL 1D5AB o|msansM 1D5AC o|
msansN 1D5AD o|msansO 1D5AE o|msansP 1D5AF o|msansQ 1D5B0 o|msansR 1D5B1 o|msansS 1D5B2 o|msansT 1D5B3 o|msansU 1D5B4 o|
msansV 1D5B5 o|msansW 1D5B6 o|msansX 1D5B7 o|msansY 1D5B8 o|msansZ 1D5B9 o|msansa 1D5BA o|msansb 1D5BB o|msansc 1D5BC o|
msansd 1D5BD o|msanse 1D5BE o|msansf 1D5BF o|msansg 1D5C0 o|msansh 1D5C1 o|msansi 1D5C2 o|msansj 1D5C3 o|msansk 1D5C4 o|
msansl 1D5C5 o|msansm 1D5C6 o|msansn 1D5C7 o|msanso 1D5C8 o|msansp 1D5C9 o|msansq 1D5CA o|msansr 1D5CB o|msanss 1D5CC o|
msanst 1D5CD o|msansu 1D5CE o|msansv 1D5CF o|msansw 1D5D0 o|msansx 1D5D1 o|msansy 1D5D2 o|msansz 1D5D3 o|mbfsansA 1D5D4 o|
mbfsansB 1D5D5 o|mbfsansC 1D5D6 o|mbfsansD 1D5D7 o|mbfsansE 1D5D8 o|mbfsansF 1D5D9 o|mbfsansG 1D5DA o|mbfsansH 1D5DB o|
mbfsansI 1D5DC o|mbfsansJ 1D5DD o|mbfsansK 1D5DE o|mbfsansL 1D5DF o|mbfsansM 1D5E0 o|mbfsansN 1D5E1 o|mbfsansO 1D5E2 o|
mbfsansP 1D5E3 o|mbfsansQ 1D5E4 o|mbfsansR 1D5E5 o|mbfsansS 1D5E6 o|mbfsansT 1D5E7 o|mbfsansU 1D5E8 o|mbfsansV 1D5E9 o|
mbfsansW 1D5EA o|mbfsansX 1D5EB o|mbfsansY 1D5EC o|mbfsansZ 1D5ED o|mbfsansa 1D5EE o|mbfsansb 1D5EF o|mbfsansc 1D5F0 o|
mbfsansd 1D5F1 o|mbfsanse 1D5F2 o|mbfsansf 1D5F3 o|mbfsansg 1D5F4 o|mbfsansh 1D5F5 o|mbfsansi 1D5F6 o|mbfsansj 1D5F7 o|
mbfsansk 1D5F8 o|mbfsansl 1D5F9 o|mbfsansm 1D5FA o|mbfsansn 1D5FB o|mbfsanso 1D5FC o|mbfsansp 1D5FD o|mbfsansq 1D5FE o|
mbfsansr 1D5FF o|mbfsanss 1D600 o|mbfsanst 1D601 o|mbfsansu 1D602 o|mbfsansv 1D603 o|mbfsansw 1D604 o|mbfsansx 1D605 o|
mbfsansy 1D606 o|mbfsansz 1D607 o|mitsansA 1D608 o|mitsansB 1D609 o|mitsansC 1D60A o|mitsansD 1D60B o|mitsansE 1D60C o|
mitsansF 1D60D o|mitsansG 1D60E o|mitsansH 1D60F o|mitsansI 1D610 o|mitsansJ 1D611 o|mitsansK 1D612 o|mitsansL 1D613 o|
mitsansM 1D614 o|mitsansN 1D615 o|mitsansO 1D616 o|mitsansP 1D617 o|mitsansQ 1D618 o|mitsansR 1D619 o|mitsansS 1D61A o|
mitsansT 1D61B o|mitsansU 1D61C o|mitsansV 1D61D o|mitsansW 1D61E o|mitsansX 1D61F o|mitsansY 1D620 o|mitsansZ 1D621 o|
mitsansa 1D622 o|mitsansb 1D623 o|mitsansc 1D624 o|mitsansd 1D625 o|mitsanse 1D626 o|mitsansf 1D627 o|mitsansg 1D628 o|
mitsansh 1D629 o|mitsansi 1D62A o|mitsansj 1D62B o|mitsansk 1D62C o|mitsansl 1D62D o|mitsansm 1D62E o|mitsansn 1D62F o|
mitsanso 1D630 o|mitsansp 1D631 o|mitsansq 1D632 o|mitsansr 1D633 o|mitsanss 1D634 o|mitsanst 1D635 o|mitsansu 1D636 o|
mitsansv 1D637 o|mitsansw 1D638 o|mitsansx 1D639 o|mitsansy 1D63A o|mitsansz 1D63B o|mbfitsansA 1D63C o|mbfitsansB 1D63D o|
mbfitsansC 1D63E o|mbfitsansD 1D63F o|mbfitsansE 1D640 o|mbfitsansF 1D641 o|mbfitsansG 1D642 o|mbfitsansH 1D643 o|
mbfitsansI 1D644 o|mbfitsansJ 1D645 o|mbfitsansK 1D646 o|mbfitsansL 1D647 o|mbfitsansM 1D648 o|mbfitsansN 1D649 o|
mbfitsansO 1D64A o|mbfitsansP 1D64B o|mbfitsansQ 1D64C o|mbfitsansR 1D64D o|mbfitsansS 1D64E o|mbfitsansT 1D64F o|
mbfitsansU 1D650 o|mbfitsansV 1D651 o|mbfitsansW 1D652 o|mbfitsansX 1D653 o|mbfitsansY 1D654 o|mbfitsansZ 1D655 o|
mbfitsansa 1D656 o|mbfitsansb 1D657 o|mbfitsansc 1D658 o|mbfitsansd 1D659 o|mbfitsanse 1D65A o|mbfitsansf 1D65B o|
mbfitsansg 1D65C o|mbfitsansh 1D65D o|mbfitsansi 1D65E o|mbfitsansj 1D65F o|mbfitsansk 1D660 o|mbfitsansl 1D661 o|
mbfitsansm 1D662 o|mbfitsansn 1D663 o|mbfitsanso 1D664 o|mbfitsansp 1D665 o|mbfitsansq 1D666 o|mbfitsansr 1D667 o|
mbfitsanss 1D668 o|mbfitsanst 1D669 o|mbfitsansu 1D66A o|mbfitsansv 1D66B o|mbfitsansw 1D66C o|mbfitsansx 1D66D o|
mbfitsansy 1D66E o|mbfitsansz 1D66F o|mttA 1D670 o|mttB 1D671 o|mttC 1D672 o|mttD 1D673 o|mttE 1D674 o|mttF 1D675 o|mttG 1D676 o|
mttH 1D677 o|mttI 1D678 o|mttJ 1D679 o|mttK 1D67A o|mttL 1D67B o|mttM 1D67C o|mttN 1D67D o|mttO 1D67E o|mttP 1D67F o|mttQ 1D680 o|
mttR 1D681 o|mttS 1D682 o|mttT 1D683 o|mttU 1D684 o|mttV 1D685 o|mttW 1D686 o|mttX 1D687 o|mttY 1D688 o|mttZ 1D689 o|mtta 1D68A o|
mttb 1D68B o|mttc 1D68C o|mttd 1D68D o|mtte 1D68E o|mttf 1D68F o|mttg 1D690 o|mtth 1D691 o|mtti 1D692 o|mttj 1D693 o|mttk 1D694 o|
mttl 1D695 o|mttm 1D696 o|mttn 1D697 o|mtto 1D698 o|mttp 1D699 o|mttq 1D69A o|mttr 1D69B o|mtts 1D69C o|mttt 1D69D o|mttu 1D69E o|
mttv 1D69F o|mttw 1D6A0 o|mttx 1D6A1 o|mtty 1D6A2 o|mttz 1D6A3 o|imath 1D6A4 o|jmath 1D6A5 o|mbfAlpha 1D6A8 o|mbfBeta 1D6A9 o|
mbfGamma 1D6AA o|mbfDelta 1D6AB o|mbfEpsilon 1D6AC o|mbfZeta 1D6AD o|mbfEta 1D6AE o|mbfTheta 1D6AF o|mbfIota 1D6B0 o|
mbfKappa 1D6B1 o|mbfLambda 1D6B2 o|mbfMu 1D6B3 o|mbfNu 1D6B4 o|mbfXi 1D6B5 o|mbfOmicron 1D6B6 o|mbfPi 1D6B7 o|mbfRho 1D6B8 o|
mbfvarTheta 1D6B9 o|mbfSigma 1D6BA o|mbfTau 1D6BB o|mbfUpsilon 1D6BC o|mbfPhi 1D6BD o|mbfChi 1D6BE o|mbfPsi 1D6BF o|
mbfOmega 1D6C0 o|mbfnabla 1D6C1 o|mbfalpha 1D6C2 o|mbfbeta 1D6C3 o|mbfgamma 1D6C4 o|mbfdelta 1D6C5 o|mbfvarepsilon 1D6C6 o|
mbfzeta 1D6C7 o|mbfeta 1D6C8 o|mbftheta 1D6C9 o|mbfiota 1D6CA o|mbfkappa 1D6CB o|mbflambda 1D6CC o|mbfmu 1D6CD o|mbfnu 1D6CE o|
mbfxi 1D6CF o|mbfomicron 1D6D0 o|mbfpi 1D6D1 o|mbfrho 1D6D2 o|mbfvarsigma 1D6D3 o|mbfsigma 1D6D4 o|mbftau 1D6D5 o|
mbfupsilon 1D6D6 o|mbfvarphi 1D6D7 o|mbfchi 1D6D8 o|mbfpsi 1D6D9 o|mbfomega 1D6DA o|mbfpartial 1D6DB o|mbfepsilon 1D6DC o|
mbfvartheta 1D6DD o|mbfvarkappa 1D6DE o|mbfphi 1D6DF o|mbfvarrho 1D6E0 o|mbfvarpi 1D6E1 o|mitAlpha 1D6E2 o|mitBeta 1D6E3 o|
mitGamma 1D6E4 o|mitDelta 1D6E5 o|mitEpsilon 1D6E6 o|mitZeta 1D6E7 o|mitEta 1D6E8 o|mitTheta 1D6E9 o|mitIota 1D6EA o|
mitKappa 1D6EB o|mitLambda 1D6EC o|mitMu 1D6ED o|mitNu 1D6EE o|mitXi 1D6EF o|mitOmicron 1D6F0 o|mitPi 1D6F1 o|mitRho 1D6F2 o|
mitvarTheta 1D6F3 o|mitSigma 1D6F4 o|mitTau 1D6F5 o|mitUpsilon 1D6F6 o|mitPhi 1D6F7 o|mitChi 1D6F8 o|mitPsi 1D6F9 o|
mitOmega 1D6FA o|mitnabla 1D6FB o|mitalpha 1D6FC o|mitbeta 1D6FD o|mitgamma 1D6FE o|mitdelta 1D6FF o|mitvarepsilon 1D700 o|
mitzeta 1D701 o|miteta 1D702 o|mittheta 1D703 o|mitiota 1D704 o|mitkappa 1D705 o|mitlambda 1D706 o|mitmu 1D707 o|mitnu 1D708 o|
mitxi 1D709 o|mitomicron 1D70A o|mitpi 1D70B o|mitrho 1D70C o|mitvarsigma 1D70D o|mitsigma 1D70E o|mittau 1D70F o|
mitupsilon 1D710 o|mitvarphi 1D711 o|mitchi 1D712 o|mitpsi 1D713 o|mitomega 1D714 o|mitpartial 1D715 o|mitepsilon 1D716 o|
mitvartheta 1D717 o|mitvarkappa 1D718 o|mitphi 1D719 o|mitvarrho 1D71A o|mitvarpi 1D71B o|mbfitAlpha 1D71C o|mbfitBeta 1D71D o|
mbfitGamma 1D71E o|mbfitDelta 1D71F o|mbfitEpsilon 1D720 o|mbfitZeta 1D721 o|mbfitEta 1D722 o|mbfitTheta 1D723 o|mbfitIota 1D724 o|
mbfitKappa 1D725 o|mbfitLambda 1D726 o|mbfitMu 1D727 o|mbfitNu 1D728 o|mbfitXi 1D729 o|mbfitOmicron 1D72A o|mbfitPi 1D72B o|
mbfitRho 1D72C o|mbfitvarTheta 1D72D o|mbfitSigma 1D72E o|mbfitTau 1D72F o|mbfitUpsilon 1D730 o|mbfitPhi 1D731 o|mbfitChi 1D732 o|
mbfitPsi 1D733 o|mbfitOmega 1D734 o|mbfitnabla 1D735 o|mbfitalpha 1D736 o|mbfitbeta 1D737 o|mbfitgamma 1D738 o|mbfitdelta 1D739 o|
mbfitvarepsilon 1D73A o|mbfitzeta 1D73B o|mbfiteta 1D73C o|mbfittheta 1D73D o|mbfitiota 1D73E o|mbfitkappa 1D73F o|
mbfitlambda 1D740 o|mbfitmu 1D741 o|mbfitnu 1D742 o|mbfitxi 1D743 o|mbfitomicron 1D744 o|mbfitpi 1D745 o|mbfitrho 1D746 o|
mbfitvarsigma 1D747 o|mbfitsigma 1D748 o|mbfittau 1D749 o|mbfitupsilon 1D74A o|mbfitvarphi 1D74B o|mbfitchi 1D74C o|
mbfitpsi 1D74D o|mbfitomega 1D74E o|mbfitpartial 1D74F o|mbfitepsilon 1D750 o|mbfitvartheta 1D751 o|mbfitvarkappa 1D752 o|
mbfitphi 1D753 o|mbfitvarrho 1D754 o|mbfitvarpi 1D755 o|mbfsansAlpha 1D756 o|mbfsansBeta 1D757 o|mbfsansGamma 1D758 o|
mbfsansDelta 1D759 o|mbfsansEpsilon 1D75A o|mbfsansZeta 1D75B o|mbfsansEta 1D75C o|mbfsansTheta 1D75D o|mbfsansIota 1D75E o|
mbfsansKappa 1D75F o|mbfsansLambda 1D760 o|mbfsansMu 1D761 o|mbfsansNu 1D762 o|mbfsansXi 1D763 o|mbfsansOmicron 1D764 o|
mbfsansPi 1D765 o|mbfsansRho 1D766 o|mbfsansvarTheta 1D767 o|mbfsansSigma 1D768 o|mbfsansTau 1D769 o|mbfsansUpsilon 1D76A o|
mbfsansPhi 1D76B o|mbfsansChi 1D76C o|mbfsansPsi 1D76D o|mbfsansOmega 1D76E o|mbfsansnabla 1D76F o|mbfsansalpha 1D770 o|
mbfsansbeta 1D771 o|mbfsansgamma 1D772 o|mbfsansdelta 1D773 o|mbfsansvarepsilon 1D774 o|mbfsanszeta 1D775 o|mbfsanseta 1D776 o|
mbfsanstheta 1D777 o|mbfsansiota 1D778 o|mbfsanskappa 1D779 o|mbfsanslambda 1D77A o|mbfsansmu 1D77B o|mbfsansnu 1D77C o|
mbfsansxi 1D77D o|mbfsansomicron 1D77E o|mbfsanspi 1D77F o|mbfsansrho 1D780 o|mbfsansvarsigma 1D781 o|mbfsanssigma 1D782 o|
mbfsanstau 1D783 o|mbfsansupsilon 1D784 o|mbfsansvarphi 1D785 o|mbfsanschi 1D786 o|mbfsanspsi 1D787 o|mbfsansomega 1D788 o|
mbfsanspartial 1D789 o|mbfsansepsilon 1D78A o|mbfsansvartheta 1D78B o|mbfsansvarkappa 1D78C o|mbfsansphi 1D78D o|
mbfsansvarrho 1D78E o|mbfsansvarpi 1D78F o|mbfitsansAlpha 1D790 o|mbfitsansBeta 1D791 o|mbfitsansGamma 1D792 o|
mbfitsansDelta 1D793 o|mbfitsansEpsilon 1D794 o|mbfitsansZeta 1D795 o|mbfitsansEta 1D796 o|mbfitsansTheta 1D797 o|
mbfitsansIota 1D798 o|mbfitsansKappa 1D799 o|mbfitsansLambda 1D79A o|mbfitsansMu 1D79B o|mbfitsansNu 1D79C o|mbfitsansXi 1D79D o|
mbfitsansOmicron 1D79E o|mbfitsansPi 1D79F o|mbfitsansRho 1D7A0 o|mbfitsansvarTheta 1D7A1 o|mbfitsansSigma 1D7A2 o|
mbfitsansTau 1D7A3 o|mbfitsansUpsilon 1D7A4 o|mbfitsansPhi 1D7A5 o|mbfitsansChi 1D7A6 o|mbfitsansPsi 1D7A7 o|
mbfitsansOmega 1D7A8 o|mbfitsansnabla 1D7A9 o|mbfitsansalpha 1D7AA o|mbfitsansbeta 1D7AB o|mbfitsansgamma 1D7AC o|
mbfitsansdelta 1D7AD o|mbfitsansvarepsilon 1D7AE o|mbfitsanszeta 1D7AF o|mbfitsanseta 1D7B0 o|mbfitsanstheta 1D7B1 o|
mbfitsansiota 1D7B2 o|mbfitsanskappa 1D7B3 o|mbfitsanslambda 1D7B4 o|mbfitsansmu 1D7B5 o|mbfitsansnu 1D7B6 o|mbfitsansxi 1D7B7 o|
mbfitsansomicron 1D7B8 o|mbfitsanspi 1D7B9 o|mbfitsansrho 1D7BA o|mbfitsansvarsigma 1D7BB o|mbfitsanssigma 1D7BC o|
mbfitsanstau 1D7BD o|mbfitsansupsilon 1D7BE o|mbfitsansvarphi 1D7BF o|mbfitsanschi 1D7C0 o|mbfitsanspsi 1D7C1 o|
mbfitsansomega 1D7C2 o|mbfitsanspartial 1D7C3 o|mbfitsansepsilon 1D7C4 o|mbfitsansvartheta 1D7C5 o|mbfitsansvarkappa 1D7C6 o|
mbfitsansphi 1D7C7 o|mbfitsansvarrho 1D7C8 o|mbfitsansvarpi 1D7C9 o|mbfDigamma 1D7CA o|mbfdigamma 1D7CB o|mbfzero 1D7CE o|
mbfone 1D7CF o|mbftwo 1D7D0 o|mbfthree 1D7D1 o|mbffour 1D7D2 o|mbffive 1D7D3 o|mbfsix 1D7D4 o|mbfseven 1D7D5 o|mbfeight 1D7D6 o|
mbfnine 1D7D7 o|Bbbzero 1D7D8 o|Bbbone 1D7D9 o|Bbbtwo 1D7DA o|Bbbthree 1D7DB o|Bbbfour 1D7DC o|Bbbfive 1D7DD o|Bbbsix 1D7DE o|
Bbbseven 1D7DF o|Bbbeight 1D7E0 o|Bbbnine 1D7E1 o|msanszero 1D7E2 o|msansone 1D7E3 o|msanstwo 1D7E4 o|msansthree 1D7E5 o|
msansfour 1D7E6 o|msansfive 1D7E7 o|msanssix 1D7E8 o|msansseven 1D7E9 o|msanseight 1D7EA o|msansnine 1D7EB o|mbfsanszero 1D7EC o|
mbfsansone 1D7ED o|mbfsanstwo 1D7EE o|mbfsansthree 1D7EF o|mbfsansfour 1D7F0 o|mbfsansfive 1D7F1 o|mbfsanssix 1D7F2 o|
mbfsansseven 1D7F3 o|mbfsanseight 1D7F4 o|mbfsansnine 1D7F5 o|mttzero 1D7F6 o|mttone 1D7F7 o|mtttwo 1D7F8 o|mttthree 1D7F9 o|
mttfour 1D7FA o|mttfive 1D7FB o|mttsix 1D7FC o|mttseven 1D7FD o|mtteight 1D7FE o|mttnine 1D7FF o|arabicmaj 1EEF0 L|
arabichad 1EEF1 L|
";

        private static void RegisterUnicodeMath()
        {
            foreach (var raw in UnicodeMathTable.Split('|'))
            {
                var parts = raw.Trim().Split(' ');
                if (parts.Length != 3) continue;
                var name = @"\" + parts[0];
                if (Math.ContainsKey(name) || BigOps.ContainsKey(name) || OperatorNames.ContainsKey(name) || Accents.ContainsKey(name)) continue;
                int cp = int.Parse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var text = char.ConvertFromUtf32(cp);
                switch (parts[2])
                {
                    case "L": Op(parts[0], text, null); break;
                    case "I": Op(parts[0], text, false, true); break;
                    case "A": Acc(parts[0], cp); break;
                    case "W": Acc(parts[0], cp, true); break;
                    case "U": Acc(parts[0], cp, false, true); break;
                    case "V": Acc(parts[0], cp, true, true); break;
                    case "X": Acc(parts[0], cp, true, false, true); break;
                    case "Y": Acc(parts[0], cp, true, true, true); break;
                    default:
                        var cls = parts[2] switch
                        {
                            "b" => AtomClass.Bin, "r" => AtomClass.Rel, "O" => AtomClass.Open, "C" => AtomClass.Close, "p" => AtomClass.Punct, _ => AtomClass.Ord,
                        };
                        M(cls, parts[0], text);
                        break;
                }
            }
        }
    }
}
