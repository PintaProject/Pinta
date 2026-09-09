import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadLocaleInventory, SEO_LOCALE_CODES } from './i18n-config.mjs';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const checkOnly = process.argv.includes('--check');
const origin = 'https://paint.rip';
const inventory = loadLocaleInventory(root);
const runtimeLocaleMeta = Object.fromEntries(inventory.locales.map((locale) => [locale.code, locale]));

const localeMeta = {
  en: { name: 'English', direction: 'ltr', ogLocale: 'en_US' },
  fr: { name: 'Français', direction: 'ltr', ogLocale: 'fr_FR' },
  de: { name: 'Deutsch', direction: 'ltr', ogLocale: 'de_DE' },
  ar: { name: 'العربية', direction: 'rtl', ogLocale: 'ar_SA' },
  he: { name: 'עברית', direction: 'rtl', ogLocale: 'he_IL' },
};

const copy = {
  fr: {
    editorTitle: 'Pinta Online – Éditeur d’images gratuit dans le navigateur | Paint.rip',
    editorOgTitle: 'Pinta Online – Éditeur d’images gratuit dans le navigateur',
    editorDescription:
      'Modifiez des images en ligne avec les outils familiers de Pinta : calques, sélections, texte, 55 effets intégrés et facultatifs, OpenRaster, raccourcis clavier et application hors ligne.',
    editorOgDescription:
      'Dessinez, sélectionnez, superposez, retouchez et exportez dans un espace de travail familier, directement dans votre navigateur.',
    editorImageAlt: 'L’éditeur Pinta Online avec boîte à outils, canevas, calques, historique et palette de couleurs.',
    languageLabel: 'Choisir la langue',
    nav: ['Fonctionnalités', 'Captures', 'Formats', 'FAQ'],
    openEditor: 'Ouvrir l’éditeur',
    skip: 'Aller au contenu',
    aboutTitle: 'Fonctionnalités de Pinta Online – Éditeur d’images web gratuit | Paint.rip',
    aboutOgTitle: 'Pinta Online – Un véritable éditeur d’images dans votre navigateur',
    aboutDescription:
      'Découvrez Pinta Online, l’éditeur d’images gratuit avec outils de dessin, calques, sélections, texte, 55 effets intégrés et facultatifs, OpenRaster et mode hors ligne.',
    aboutOgDescription:
      'Dessinez, sélectionnez, superposez, écrivez, transformez et exportez sans installer un éditeur de bureau.',
    hero: {
      eyebrow: 'Éditeur d’images gratuit dans le navigateur',
      title: ['L’âme de Pinta sur ordinateur,', 'prête dans votre navigateur.'],
      lead: 'Dessinez, retouchez, sélectionnez, organisez en calques, ajoutez du texte, transformez et exportez dans un espace familier, sans installation ni envoi de vos fichiers vers un serveur.',
      start: 'Commencer à dessiner',
      see: 'Voir l’interface',
      trust: ['Gratuit', 'Traitement local', 'Installable et hors ligne'],
      caption: 'L’espace de travail complet sur paint.rip',
    },
    stats: [
      'outils de dessin et d’édition',
      'modes de fusion natifs',
      'ajustements et effets',
      'formats d’image ouverts',
    ],
    features: {
      eyebrow: 'De vrais outils d’édition',
      title: 'Tout ce que vous attendez d’un éditeur d’images.',
      lead: 'Pinta Online réunit les flux essentiels de dessin, d’image, de calques, de sélection et d’effets dans une application web réactive.',
      cards: [
        [
          '✦',
          'Dessiner et retoucher',
          'Utilisez pinceau, crayon, gomme, remplissage, dégradés, tampon de clonage, recoloration et pipette avec largeur et tolérance réglables.',
        ],
        [
          '◫',
          'Sélectionner précisément',
          'Créez des sélections rectangulaires, elliptiques, au lasso, polygonales ou par baguette magique, puis combinez-les, déplacez-les ou recadrez.',
        ],
        [
          '▱',
          'Travailler en calques',
          'Créez, dupliquez, fusionnez, réordonnez, masquez et transformez les calques avec opacité, miniatures et 16 modes de fusion.',
        ],
        [
          'T',
          'Texte et formes',
          'Modifiez du texte multiligne sur le canevas et dessinez lignes, courbes, rectangles, ellipses et formes libres avec des poignées actives.',
        ],
        [
          'ƒ',
          'Ajuster et transformer',
          'Réglez courbes, niveaux, teinte et contraste, puis explorez flous, bruit, distorsions, traitements artistiques et outils photo.',
        ],
        [
          '↺',
          'Annuler sans crainte',
          'Chaque document garde un historique complet tandis que ses calques, sa sélection, son zoom, son nom et son état restent indépendants.',
        ],
      ],
    },
    screenshots: {
      eyebrow: 'La véritable interface',
      title: 'Un espace de travail qui se fait oublier.',
      lead: 'Toutes ces images proviennent de la même suite Playwright figée qui protège la fidélité visuelle de l’éditeur.',
      rows: [
        [
          'Manipulation directe',
          'Le texte se modifie sur le canevas.',
          'Placez du texte multiligne, déplacez-le et réglez famille, taille, graisse, alignement, remplissage, contour et arrière-plan avant de valider.',
          [
            'Édition directe sur le canevas',
            'Styles de remplissage, contour et arrière-plan',
            'Validation au clavier et contrôles typographiques',
          ],
          'Texte multiligne avec contrôles typographiques en direct',
        ],
        [
          'Sélections adaptées aux pixels',
          'Sélectionnez, combinez, déplacez et affinez.',
          'Combinez les zones par remplacement, union, exclusion, XOR ou intersection, puis coupez, copiez, collez, décalez, recadrez ou transformez.',
          [
            'Rectangle, ellipse, lasso, polygone et baguette magique',
            'Modes de sélection avec touches modificatrices',
            'Effets et ajustements limités à la sélection',
          ],
          'Les sélections s’intègrent aux calques, à l’historique et aux effets',
        ],
        [
          'Une riche bibliothèque d’effets',
          'Des corrections subtiles aux transformations radicales.',
          'Affinez Courbes et Niveaux, corrigez les photos, générez textures et fractales ou explorez flou, bruit, distorsion, rendu et stylisation.',
          [
            '46 effets intégrés plus 9 effets facultatifs exécutés en arrière-plan',
            'Boîtes de paramètres avec aperçu',
            'Historique déterministe après chaque application',
          ],
          'Les effets reprennent les catégories familières de Pinta',
        ],
      ],
      details: [
        ['Courbes', 'Spline cubique naturelle par canal'],
        ['Nuages', 'Bruit procédural en couches'],
        ['Peinture à l’huile', 'Traitement artistique réglable'],
        ['Pilotage au clavier', 'Les raccourcis Pinta prennent le pas sur le navigateur'],
      ],
    },
    formats: {
      eyebrow: 'Formats ouverts, fichiers locaux',
      title: 'Vos images restent utiles — et restent les vôtres.',
      lead: 'Pinta Online traite les images dans votre navigateur. Ouvrez plusieurs fichiers dans des onglets indépendants et exportez-les dans le format adapté à la suite de votre travail.',
      open: 'Ouvrir',
      save: 'Enregistrer',
      theme: 'Les thèmes sombre et clair mémorisent votre préférence.',
    },
    local: {
      eyebrow: 'Application web progressive locale',
      title: ['Ouvrez-la comme un site.', 'Utilisez-la comme une application.'],
      lead: 'Installez Pinta Online pour obtenir une fenêtre dédiée et un démarrage hors ligne. IndexedDB restaure localement documents, calques, pixels, sélections, zoom et onglets.',
      points: [
        ['Sans compte', 'Ouvrez l’éditeur et commencez immédiatement.'],
        ['Sans étape d’envoi', 'L’édition et les effets s’exécutent dans le navigateur.'],
        ['Restauration complète', 'Retrouvez documents, calques et historique.'],
        ['Prêt hors ligne', 'L’interface et les illustrations sont mises en cache.'],
      ],
    },
    faq: {
      eyebrow: 'Bon à savoir',
      title: 'Questions fréquentes.',
      items: [
        ['Pinta Online est-il gratuit ?', 'Oui. L’éditeur de paint.rip est gratuit et ne nécessite aucun compte.'],
        [
          'Mes images sont-elles envoyées à un serveur ?',
          'Non. Le décodage, la composition, les transformations et l’export se font dans votre navigateur. La restauration est conservée localement sur cet appareil.',
        ],
        [
          'Peut-il conserver les calques ?',
          'Oui. L’import et l’export OpenRaster conservent noms, visibilité, opacité, modes de fusion, ordre et pixels des calques.',
        ],
        [
          'Utilise-t-il les raccourcis Pinta ?',
          'Oui. Ctrl/Commande+N, O, S, W, R et les touches d’outils d’origine sont capturés par l’éditeur.',
        ],
        [
          'Puis-je l’installer ?',
          'Oui. Un navigateur compatible peut installer Pinta Online comme application web progressive et lancer son interface hors ligne.',
        ],
      ],
    },
    final: {
      eyebrow: 'Rien à installer d’abord',
      title: 'Votre prochain canevas est à un clic.',
      lead: 'Ouvrez Pinta Online, appuyez sur Ctrl/Commande+N et créez quelque chose.',
      button: 'Lancer Pinta Online',
    },
    footer: {
      description: 'Un éditeur d’images natif du navigateur, basé sur le projet libre Pinta.',
      portedBy: 'Porté sur le Web par',
      editor: 'Éditeur',
      source: 'Code web',
      project: 'Projet Pinta',
      issue: 'Signaler un problème',
      copyright: 'Copyright © 2010–2026 contributeurs de Pinta. Distribué sous licence MIT X11.',
    },
    featureList: [
      '23 outils de dessin et d’édition disponibles',
      'Calques avec 16 modes de fusion',
      '55 ajustements et effets intégrés et facultatifs',
      'Import et export OpenRaster',
      'Application web progressive hors ligne',
    ],
    popart: {
      eyebrow: 'Un monde, dix traitements',
      title: 'Une œuvre. Des résultats radicalement différents.',
      intro:
        'Nous avons ouvert une illustration originale Cosmic Garden dans Pinta Online, puis ajouté un titre éditable, un halo tracé avec l’outil forme et une ligne graphique sur des calques séparés. Une copie aplatie a ensuite parcouru la bibliothèque, un effet à la fois. Le sujet reste fixe : chaque image ne diffère que par l’outil qu’elle démontre.',
      heroAlt:
        'L’éditeur Pinta Online avec une scène surréaliste de carpe koï, portail lunaire et astronaute, ainsi que quatre calques nommés dans le panneau Calques.',
      heroCaption: 'Une illustration originale et trois calques Pinta éditables : halo, ligne graphique et texte',
      note: 'Cinq de ces effets sont désactivés par défaut. Pixellisation hexagonale, Aberration chromatique, Lignes de balayage, Artefacts colorés et Vision nocturne proviennent d’extensions fournies que vous activez dans le gestionnaire d’extensions — c’est ainsi que cette grille a été produite, et ainsi que vous produiriez la vôtre.',
      untreated: 'Sans traitement',
      labels: [
        'Illustration importée, ellipse, ligne, texte éditable',
        'Une mosaïque créée par une extension',
        'Un dessin aux traits de roman graphique',
        'Canaux décalés de 14 px',
        'Trame cathodique',
        '128 blocs générés',
        'Trois niveaux par canal',
        'Le tirage en négatif',
        'Réponse verte et grain',
        'Taille et granulométrie du pinceau',
      ],
    },
  },
  de: {
    editorTitle: 'Pinta Online – Kostenloser Bildeditor im Browser | Paint.rip',
    editorOgTitle: 'Pinta Online – Kostenloser Bildeditor im Browser',
    editorDescription:
      'Bilder online mit Pintas vertrauten Werkzeugen bearbeiten: Ebenen, Auswahl, Text, 55 integrierte und optionale Effekte, OpenRaster, Tastenkürzel und eine installierbare Offline-App.',
    editorOgDescription:
      'Malen, auswählen, Ebenen nutzen, retuschieren und exportieren – in einer vertrauten Arbeitsfläche direkt im Browser.',
    editorImageAlt: 'Der Pinta-Online-Editor mit Werkzeugkasten, Leinwand, Ebenen, Verlauf und Farbpalette.',
    languageLabel: 'Sprache wählen',
    nav: ['Funktionen', 'Screenshots', 'Formate', 'FAQ'],
    openEditor: 'Editor öffnen',
    skip: 'Zum Inhalt springen',
    aboutTitle: 'Pinta-Online-Funktionen – Kostenloser Web-Bildeditor | Paint.rip',
    aboutOgTitle: 'Pinta Online – Ein vollwertiger Bildeditor im Browser',
    aboutDescription:
      'Entdecke Pinta Online, den kostenlosen Bildeditor mit Zeichenwerkzeugen, Ebenen, Auswahl, Text, 55 integrierten und optionalen Effekten, OpenRaster und Offline-Modus.',
    aboutOgDescription:
      'Malen, auswählen, Ebenen nutzen, schreiben, transformieren und exportieren – ohne klassischen Desktop-Editor.',
    hero: {
      eyebrow: 'Kostenloser Bildeditor im Browser',
      title: ['Pintas Desktop-Seele,', 'bereit in deinem Browser.'],
      lead: 'Male, retuschiere, wähle aus, arbeite mit Ebenen, Text und Transformationen und exportiere in einer vertrauten Umgebung – ohne Installation und ohne Upload deiner Arbeitsdateien.',
      start: 'Jetzt malen',
      see: 'Oberfläche ansehen',
      trust: ['Kostenlos', 'Lokale Bearbeitung', 'Installierbar und offline'],
      caption: 'Der vollständige Arbeitsbereich auf paint.rip',
    },
    stats: [
      'Zeichen- und Bearbeitungswerkzeuge',
      'native Mischmodi',
      'Anpassungen und Effekte',
      'geöffnete Bildformate',
    ],
    features: {
      eyebrow: 'Echte Bearbeitungswerkzeuge',
      title: 'Alles, was du von einem Bildeditor erwartest.',
      lead: 'Pinta Online vereint die wichtigsten Zeichen-, Bild-, Ebenen-, Auswahl- und Effektabläufe in einer reaktionsschnellen Webanwendung.',
      cards: [
        [
          '✦',
          'Zeichnen und retuschieren',
          'Nutze Pinsel, Stift, Radierer, Füllwerkzeug, Verläufe, Klonstempel, Umfärben und Farbpipette mit einstellbarer Breite und Toleranz.',
        ],
        [
          '◫',
          'Gezielt auswählen',
          'Erstelle Rechteck-, Ellipsen-, Lasso-, Polygon- und Zauberstab-Auswahlen und kombiniere, verschiebe, fülle oder beschneide sie.',
        ],
        [
          '▱',
          'Ideen auf Ebenen',
          'Erstelle, dupliziere, verbinde, sortiere, verstecke und transformiere Ebenen mit Deckkraft, Vorschaubildern und 16 Mischmodi.',
        ],
        [
          'T',
          'Text und Formen',
          'Bearbeite mehrzeiligen Text auf der Leinwand und zeichne Linien, Kurven, Rechtecke, Ellipsen und freie Formen mit interaktiven Griffen.',
        ],
        [
          'ƒ',
          'Anpassen und transformieren',
          'Arbeite mit Kurven, Tonwerten, Farbton und Kontrast sowie Unschärfe, Rauschen, Verzerrungen, Kunst- und Fotowerkzeugen.',
        ],
        [
          '↺',
          'Sicher rückgängig',
          'Jedes Dokument behält seinen gesamten Verlauf; Ebenen, Auswahl, Zoom, Dateiname und Änderungsstatus bleiben unabhängig.',
        ],
      ],
    },
    screenshots: {
      eyebrow: 'Die echte Oberfläche',
      title: 'Ein Arbeitsbereich, der nicht im Weg steht.',
      lead: 'Alle Bilder stammen aus derselben festgeschriebenen Playwright-Suite, die die visuelle Treue des Editors schützt.',
      rows: [
        [
          'Direkte Bearbeitung',
          'Text gehört direkt auf die Leinwand.',
          'Platziere mehrzeiligen Text, verschiebe ihn und ändere Schrift, Größe, Gewicht, Ausrichtung, Füllung, Kontur und Hintergrund vor dem Anwenden.',
          [
            'Direkte Bearbeitung auf der Leinwand',
            'Füll-, Kontur- und Hintergrundstile',
            'Tastaturbestätigung und Typografie-Steuerung',
          ],
          'Mehrzeiliger Text mit direkter Typografie-Steuerung',
        ],
        [
          'Pixelgenaue Auswahl',
          'Auswählen, kombinieren, verschieben und verfeinern.',
          'Kombiniere Bereiche per Ersetzen, Vereinigung, Ausschluss, XOR oder Schnittmenge und schneide, kopiere, verschiebe, beschneide oder transformiere sie.',
          [
            'Rechteck, Ellipse, Lasso, Polygon und Zauberstab',
            'Auswahlmodi mit Modifikatortasten',
            'Auswahlbezogene Effekte und Anpassungen',
          ],
          'Auswahlen arbeiten mit Ebenen, Verlauf und Effekten zusammen',
        ],
        [
          'Umfangreiche Effektbibliothek',
          'Von feinen Korrekturen bis zu wilden Transformationen.',
          'Passe Kurven und Tonwerte an, korrigiere Fotos, rendere Texturen und Fraktale oder erkunde Unschärfe, Rauschen, Verzerrung und Stilisierung.',
          [
            '46 integrierte plus 9 optionale Effekte im Hintergrund',
            'Parameterdialoge mit Vorschau',
            'Deterministischer Verlauf nach jeder Anwendung',
          ],
          'Effekte sind in vertrauten Pinta-Kategorien organisiert',
        ],
      ],
      details: [
        ['Kurven', 'Natürliche kubische Splines pro Kanal'],
        ['Wolken', 'Geschichtetes prozedurales Rauschen'],
        ['Ölgemälde', 'Einstellbare künstlerische Behandlung'],
        ['Tastatur zuerst', 'Pinta-Kürzel übersteuern Browser-Aktionen'],
      ],
    },
    formats: {
      eyebrow: 'Offene Formate, lokale Dateien',
      title: 'Deine Bilder bleiben nützlich – und bleiben deine.',
      lead: 'Pinta Online verarbeitet Bilder im Browser. Öffne mehrere Dateien in unabhängigen Tabs und exportiere sie im passenden Format für den nächsten Schritt.',
      open: 'Öffnen',
      save: 'Speichern',
      theme: 'Dunkles und helles Design merken sich deine Einstellung.',
    },
    local: {
      eyebrow: 'Lokale Progressive Web App',
      title: ['Öffnen wie eine Website.', 'Nutzen wie eine App.'],
      lead: 'Installiere Pinta Online für ein eigenes Fenster und Offline-Start. IndexedDB stellt Dokumente, Ebenen, Pixel, Auswahl, Zoom und Tabs lokal wieder her.',
      points: [
        ['Kein Konto nötig', 'Editor öffnen und sofort anfangen.'],
        ['Kein Bild-Upload', 'Bearbeitung und Effekte laufen im Browser.'],
        ['Vollständige Wiederherstellung', 'Dokumente, Ebenen und Verlauf bleiben erhalten.'],
        ['Offline bereit', 'App-Oberfläche und Grafiken werden vorgeladen.'],
      ],
    },
    faq: {
      eyebrow: 'Gut zu wissen',
      title: 'Fragen und Antworten.',
      items: [
        ['Ist Pinta Online kostenlos?', 'Ja. Der Editor auf paint.rip ist kostenlos und benötigt kein Konto.'],
        [
          'Werden meine Bilder hochgeladen?',
          'Nein. Dekodierung, Komposition, Transformation und Export finden im Browser statt. Die Wiederherstellung wird lokal auf diesem Gerät gespeichert.',
        ],
        [
          'Bleiben Ebenen erhalten?',
          'Ja. OpenRaster-Import und -Export erhalten Namen, Sichtbarkeit, Deckkraft, Mischmodi, Reihenfolge und Pixeldaten.',
        ],
        [
          'Verwendet es die Pinta-Tastenkürzel?',
          'Ja. Strg/Befehl+N, O, S, W, R und die ursprünglichen Werkzeugtasten werden vom Editor übernommen.',
        ],
        [
          'Kann ich es installieren?',
          'Ja. Ein unterstützter Browser kann Pinta Online als Progressive Web App installieren und die Oberfläche offline starten.',
        ],
      ],
    },
    final: {
      eyebrow: 'Nichts zuerst installieren',
      title: 'Deine nächste Leinwand ist einen Klick entfernt.',
      lead: 'Öffne Pinta Online, drücke Strg/Befehl+N und erschaffe etwas.',
      button: 'Pinta Online starten',
    },
    footer: {
      description: 'Ein browsernativer Bildeditor auf Basis des Open-Source-Projekts Pinta.',
      portedBy: 'Für das Web portiert von',
      editor: 'Editor',
      source: 'Web-Quellcode',
      project: 'Pinta-Projekt',
      issue: 'Problem melden',
      copyright: 'Copyright © 2010–2026 Pinta-Mitwirkende. Veröffentlicht unter der MIT-X11-Lizenz.',
    },
    featureList: [
      '23 verfügbare Zeichen- und Bearbeitungswerkzeuge',
      'Ebenen mit 16 Mischmodi',
      '55 integrierte und optionale Anpassungen und Effekte',
      'OpenRaster-Import und -Export',
      'Offline-fähige Progressive Web App',
    ],
    popart: {
      eyebrow: 'Eine Welt, zehn Bearbeitungen',
      title: 'Ein Kunstwerk. Völlig andere Ergebnisse.',
      intro:
        'Wir haben eine originale Cosmic-Garden-Illustration in Pinta Online geöffnet und auf separaten Ebenen editierbaren Text, einen mit dem Formwerkzeug gezeichneten Portalring und eine grafische Signallinie ergänzt. Eine reduzierte Kopie lief anschließend Effekt für Effekt durch die Bibliothek. Das Motiv bleibt gleich, sodass sich jedes Bild nur durch das gezeigte Werkzeug unterscheidet.',
      heroAlt:
        'Der Pinta-Online-Editor mit einer surrealen Szene aus Koi, Mondportal und Astronaut sowie vier benannten Ebenen im Ebenen-Dock.',
      heroCaption: 'Originalillustration plus drei editierbare Pinta-Ebenen: Halo, Signallinie und Text',
      note: 'Fünf davon sind standardmäßig ausgeschaltet. Sechseckig verpixeln, Chromatische Aberration, Abtastzeilen, Farbige Artefakte und Nachtsicht stammen aus mitgelieferten Add-ins, die Sie im Add-in-Manager aktivieren — so ist dieses Raster entstanden, und so würden Sie Ihr eigenes erstellen.',
      untreated: 'Unbehandelt',
      labels: [
        'Importierte Illustration, Ellipse, Linie, editierbarer Text',
        'Ein Mosaik aus einem Add-in',
        'Grafische Roman-Linien',
        'Kanäle um 14 px versetzt',
        'Bildröhren-Raster',
        '128 erzeugte Blöcke',
        'Drei Stufen pro Kanal',
        'Der Negativabzug',
        'Grüne Antwort plus Korn',
        'Pinselgröße und Körnung',
      ],
    },
  },
  ar: {
    editorTitle: 'بِنْتا أونلاين – محرر صور مجاني في المتصفح | Paint.rip',
    editorOgTitle: 'بِنْتا أونلاين – محرر صور مجاني في المتصفح',
    editorDescription:
      'حرّر الصور عبر الإنترنت بأدوات بِنْتا المألوفة: الطبقات والتحديد والنص و55 مؤثرًا مضمّنًا واختياريًا ودعم OpenRaster والاختصارات وتطبيق يعمل دون اتصال.',
    editorOgDescription: 'ارسم وحدد واستخدم الطبقات ونقّح وصدّر في مساحة عمل مألوفة، مباشرة في متصفحك.',
    editorImageAlt: 'محرر بِنْتا أونلاين وفيه صندوق الأدوات واللوحة والطبقات والسجل ولوحة الألوان.',
    languageLabel: 'اختيار اللغة',
    nav: ['الميزات', 'لقطات الشاشة', 'الصيغ', 'الأسئلة'],
    openEditor: 'افتح المحرر',
    skip: 'انتقل إلى المحتوى',
    aboutTitle: 'ميزات بِنْتا أونلاين – محرر صور مجاني للويب | Paint.rip',
    aboutOgTitle: 'بِنْتا أونلاين – محرر صور متكامل داخل متصفحك',
    aboutDescription:
      'اكتشف بِنْتا أونلاين، محرر الصور المجاني بأدوات الرسم والطبقات والتحديد والنص و55 مؤثرًا مضمّنًا واختياريًا ودعم OpenRaster والعمل دون اتصال.',
    aboutOgDescription: 'ارسم وحدد واستخدم الطبقات واكتب وحوّل وصدّر دون تثبيت محرر مكتبي تقليدي.',
    hero: {
      eyebrow: 'محرر صور مجاني في المتصفح',
      title: ['روح بِنْتا المكتبية،', 'جاهزة في متصفحك.'],
      lead: 'ارسم ونقّح وحدد واستخدم الطبقات والنص والتحويل ثم صدّر في مساحة مألوفة، بلا تثبيت وبلا إرسال ملفات عملك إلى خادم.',
      start: 'ابدأ الرسم الآن',
      see: 'شاهد الواجهة',
      trust: ['مجاني', 'تحرير محلي', 'قابل للتثبيت ويعمل دون اتصال'],
      caption: 'مساحة العمل الكاملة على paint.rip',
    },
    stats: ['أداة للرسم والتحرير', 'وضع مزج أصلي', 'تعديلًا ومؤثرًا', 'صيغ صور يمكن فتحها'],
    features: {
      eyebrow: 'أدوات تحرير حقيقية',
      title: 'كل ما تتوقعه من محرر صور.',
      lead: 'يجمع بِنْتا أونلاين أهم مسارات الرسم والصور والطبقات والتحديد والمؤثرات في تطبيق ويب سريع الاستجابة.',
      cards: [
        [
          '✦',
          'ارسم ونقّح',
          'استخدم الفرشاة والقلم والممحاة والتعبئة والتدرجات وختم الاستنساخ وإعادة التلوين والقطّارة مع عرض وتفاوت قابلين للضبط.',
        ],
        [
          '◫',
          'حدد بدقة',
          'أنشئ تحديدات مستطيلة وبيضاوية وحرة ومضلعة وبالعصا السحرية، ثم ادمجها أو حرّكها أو املأها أو اقتصّها.',
        ],
        [
          '▱',
          'نظّم أفكارك في طبقات',
          'أنشئ الطبقات وكررها وادمجها ورتبها وأخفها وحوّلها مع التحكم بالعتامة والصور المصغرة و16 وضع مزج.',
        ],
        [
          'T',
          'اكتب وارسم الأشكال',
          'حرّر نصًا متعدد الأسطر على اللوحة وارسم الخطوط والمنحنيات والمستطيلات والأشكال البيضاوية والحرة بمقابض مباشرة.',
        ],
        [
          'ƒ',
          'عدّل وحوّل',
          'استخدم المنحنيات والمستويات واللون والتباين، واستكشف التمويه والضجيج والتشويه والمعالجات الفنية وأدوات الصور.',
        ],
        ['↺', 'تراجع بثقة', 'يحتفظ كل مستند بسجله الكامل، وتبقى طبقاته وتحديده وتقريبه واسمه وحالة تعديلاته مستقلة.'],
      ],
    },
    screenshots: {
      eyebrow: 'الواجهة الفعلية',
      title: 'مساحة عمل لا تعترض طريقك.',
      lead: 'كل صورة هنا مأخوذة من حزمة لقطات Playwright المثبتة نفسها التي تحمي الدقة البصرية للمحرر.',
      rows: [
        [
          'تحكم مباشر',
          'مكان تحرير النص هو اللوحة.',
          'ضع نصًا متعدد الأسطر وحرّكه واضبط الخط والحجم والسماكة والمحاذاة والتعبئة والحد والخلفية قبل اعتماده.',
          ['تحرير مباشر على اللوحة', 'أنماط التعبئة والحد والخلفية', 'اعتماد بلوحة المفاتيح وتحكم بالخط'],
          'نص متعدد الأسطر مع تحكم مباشر بالطباعة',
        ],
        [
          'تحديد واعٍ بالبكسلات',
          'حدد وادمج وحرّك وحسّن.',
          'ادمج المناطق بالاستبدال أو الاتحاد أو الاستبعاد أو XOR أو التقاطع، ثم قصها أو انسخها أو أزحها أو اقتصّها أو حوّلها.',
          ['مستطيل وبيضاوي وحر ومضلع وعصا سحرية', 'أوضاع تحديد بمفاتيح التعديل', 'مؤثرات وتعديلات داخل التحديد'],
          'يعمل التحديد مع الطبقات والسجل والمؤثرات',
        ],
        [
          'مكتبة مؤثرات عميقة',
          'من التصحيح الدقيق إلى التحويل الجريء.',
          'اضبط المنحنيات والمستويات وصحح الصور وأنشئ خامات وكسوريات أو استكشف التمويه والضجيج والتشويه والتصيير والأسلوب.',
          ['46 مؤثرًا مضمّنًا و9 مؤثرات اختيارية في عمليات خلفية', 'حوارات إعدادات مع معاينة', 'سجل حتمي بعد كل تطبيق'],
          'المؤثرات مرتبة ضمن فئات بِنْتا المألوفة',
        ],
      ],
      details: [
        ['المنحنيات', 'منحنى تكعيبي طبيعي لكل قناة'],
        ['السحب', 'ضجيج إجرائي متعدد الطبقات'],
        ['الرسم الزيتي', 'معالجة فنية قابلة للضبط'],
        ['العمل بلوحة المفاتيح', 'اختصارات بِنْتا تتقدم على إجراءات المتصفح'],
      ],
    },
    formats: {
      eyebrow: 'صيغ مفتوحة وملفات محلية',
      title: 'تبقى صورك مفيدة — وتبقى ملكك.',
      lead: 'يعالج بِنْتا أونلاين الصور داخل متصفحك. افتح عدة ملفات في ألسنة مستقلة وصدّر كلًا منها بالصيغة المناسبة لخطوتك التالية.',
      open: 'فتح',
      save: 'حفظ',
      theme: 'يتذكر المظهران الداكن والفاتح تفضيلك.',
    },
    local: {
      eyebrow: 'تطبيق ويب تقدمي محلي',
      title: ['افتحه كموقع.', 'واستخدمه كتطبيق.'],
      lead: 'ثبّت بِنْتا أونلاين لتحصل على نافذة تطبيق وبدء دون اتصال. يعيد IndexedDB المستندات والطبقات والبكسلات والتحديد والتقريب والألسنة محليًا.',
      points: [
        ['لا يحتاج إلى حساب', 'افتح المحرر وابدأ فورًا.'],
        ['لا توجد خطوة رفع', 'التحرير والمؤثرات يعملان في المتصفح.'],
        ['استعادة كاملة', 'ارجع إلى المستندات والطبقات والسجل.'],
        ['جاهز دون اتصال', 'تُخزّن الواجهة والرسومات مسبقًا.'],
      ],
    },
    faq: {
      eyebrow: 'من المفيد أن تعرف',
      title: 'أسئلة وإجابات.',
      items: [
        ['هل بِنْتا أونلاين مجاني؟', 'نعم. محرر paint.rip مجاني ولا يتطلب حسابًا.'],
        [
          'هل تُرفع صوري إلى خادم؟',
          'لا. يجري فك الترميز والتركيب والتحويل والتصدير في متصفحك، وتُحفظ استعادة مساحة العمل محليًا على جهازك.',
        ],
        [
          'هل يحافظ على الطبقات؟',
          'نعم. يحافظ استيراد OpenRaster وتصديره على الأسماء والظهور والعتامة وأوضاع المزج والترتيب وبيانات البكسلات.',
        ],
        ['هل يستخدم اختصارات بِنْتا الأصلية؟', 'نعم. يلتقط المحرر Ctrl/Command+N وO وS وW وR ومفاتيح الأدوات الأصلية.'],
        [
          'هل أستطيع تثبيته؟',
          'نعم. يستطيع المتصفح المدعوم تثبيت بِنْتا أونلاين كتطبيق ويب تقدمي وتشغيل واجهته دون اتصال.',
        ],
      ],
    },
    final: {
      eyebrow: 'لا شيء يلزم تثبيته أولًا',
      title: 'لوحتك التالية على بُعد نقرة.',
      lead: 'افتح بِنْتا أونلاين واضغط Ctrl/Command+N وابدأ الإبداع.',
      button: 'شغّل بِنْتا أونلاين',
    },
    footer: {
      description: 'محرر صور أصيل للمتصفح مبني على مشروع بِنْتا مفتوح المصدر.',
      portedBy: 'نقله إلى الويب',
      editor: 'المحرر',
      source: 'مصدر الويب',
      project: 'مشروع بِنْتا',
      issue: 'أبلغ عن مشكلة',
      copyright: 'حقوق النشر © 2010–2026 لمساهمي بِنْتا. منشور برخصة MIT X11.',
    },
    featureList: [
      '23 أداة متاحة للرسم والتحرير',
      'طبقات مع 16 وضع مزج',
      '55 تعديلًا ومؤثرًا مضمّنًا واختياريًا',
      'استيراد OpenRaster وتصديره',
      'تطبيق ويب تقدمي يعمل دون اتصال',
    ],
    popart: {
      eyebrow: 'عالم واحد، عشر معالجات',
      title: 'عمل فني واحد. نتائج مختلفة تمامًا.',
      intro:
        'فتحنا رسماً أصلياً باسم Cosmic Garden في Pinta Online، ثم أضفنا نصاً قابلاً للتحرير وهالة للبوابة مرسومة بأداة الأشكال وخطاً بيانياً على طبقات منفصلة. بعد ذلك مررنا نسخة مدمجة عبر المكتبة، مؤثراً واحداً كل مرة. يبقى المشهد ثابتاً، لذلك لا تختلف كل صورة إلا بالأداة التي تعرضها.',
      heroAlt:
        'محرر Pinta Online وفيه مشهد سريالي لسمكة كوي وبوابة قمرية ورائد فضاء مع أربع طبقات مسماة في لوحة الطبقات.',
      heroCaption: 'رسم أصلي وثلاث طبقات Pinta قابلة للتحرير: هالة وخط بياني ونص حي',
      note: 'خمسة من هذه التأثيرات معطّلة افتراضيًا. بكسلة سداسية وانحراف لوني وخطوط المسح وتشوهات ملوّنة ورؤية ليلية تأتي من إضافات مرفقة تُفعّلها من مدير الإضافات — وبهذه الطريقة أُنشئت هذه الشبكة، وبها تُنشئ شبكتك.',
      untreated: 'بلا معالجة',
      labels: [
        'رسم مستورد وقطع ناقص وخط ونص قابل للتحرير',
        'فسيفساء من إضافة',
        'خطوط بأسلوب الرواية المصورة',
        'قنوات مزاحة ١٤ بكسل',
        'تسطير الشاشة',
        '١٢٨ كتلة مولّدة',
        'ثلاثة مستويات لكل قناة',
        'الطبعة السالبة',
        'استجابة خضراء وحُبيبات',
        'حجم الفرشاة وخشونتها',
      ],
    },
  },
  he: {
    editorTitle: 'Pinta Online – עורך תמונות חינמי בדפדפן | Paint.rip',
    editorOgTitle: 'Pinta Online – עורך תמונות חינמי בדפדפן',
    editorDescription:
      'עריכת תמונות מקוונת עם הכלים המוכרים של Pinta: שכבות, בחירות, טקסט, 55 אפקטים מובנים ואופציונליים, OpenRaster, קיצורי מקלדת ואפליקציה לא מקוונת.',
    editorOgDescription: 'ציירו, בחרו, עבדו בשכבות, רטשו וייצאו בסביבת עבודה מוכרת ישירות בדפדפן.',
    editorImageAlt: 'עורך Pinta Online עם ארגז כלים, בד ציור, שכבות, היסטוריה ולוח צבעים.',
    languageLabel: 'בחירת שפה',
    nav: ['תכונות', 'צילומי מסך', 'פורמטים', 'שאלות'],
    openEditor: 'פתיחת העורך',
    skip: 'דילוג לתוכן',
    aboutTitle: 'תכונות Pinta Online – עורך תמונות חינמי לרשת | Paint.rip',
    aboutOgTitle: 'Pinta Online – עורך תמונות מלא בתוך הדפדפן',
    aboutDescription:
      'הכירו את Pinta Online, עורך התמונות החינמי עם כלי ציור, שכבות, בחירות, טקסט, 55 אפקטים מובנים ואופציונליים, OpenRaster ועבודה לא מקוונת.',
    aboutOgDescription: 'ציירו, בחרו, עבדו בשכבות, הקלידו, שנו וייצאו בלי להתקין עורך שולחני מסורתי.',
    hero: {
      eyebrow: 'עורך תמונות חינמי בדפדפן',
      title: ['הנשמה השולחנית של Pinta,', 'מוכנה בדפדפן שלכם.'],
      lead: 'ציירו, רטשו, בחרו, עבדו בשכבות, הוסיפו טקסט, שנו וייצאו בסביבה מוכרת — בלי התקנה ובלי לשלוח את קובצי העבודה לשרת.',
      start: 'להתחיל לצייר',
      see: 'לראות את הממשק',
      trust: ['חינם לשימוש', 'עריכה מקומית', 'ניתן להתקנה ולא מקוון'],
      caption: 'סביבת העבודה המלאה ב־paint.rip',
    },
    stats: ['כלי ציור ועריכה', 'מצבי שילוב מקוריים', 'התאמות ואפקטים', 'פורמטים שניתן לפתוח'],
    features: {
      eyebrow: 'כלי עריכה אמיתיים',
      title: 'כל מה שמצפים מעורך תמונות.',
      lead: 'Pinta Online מרכז את תהליכי הציור, התמונה, השכבות, הבחירה והאפקטים החשובים ביישום רשת מגיב אחד.',
      cards: [
        [
          '✦',
          'ציור וריטוש',
          'השתמשו במכחול, עיפרון, מחק, מילוי, מעברי צבע, חותמת שיבוט, צביעה מחדש ודוגם צבע עם רוחב וסבילות מתכווננים.',
        ],
        [
          '◫',
          'בחירה מדויקת',
          'צרו בחירות מלבניות, אליפטיות, חופשיות, מצולעות ובמטה קסם, ואז שלבו, הזיזו, מלאו או חתכו אותן.',
        ],
        [
          '▱',
          'רעיונות בשכבות',
          'צרו, שכפלו, מזגו, סדרו, הסתירו ושנו שכבות עם אטימות, תמונות ממוזערות ו־16 מצבי שילוב.',
        ],
        [
          'T',
          'טקסט וצורות',
          'ערכו טקסט מרובה שורות על הבד וציירו קווים, עקומות, מלבנים, אליפסות וצורות חופשיות עם ידיות פעילות.',
        ],
        [
          'ƒ',
          'התאמה ושינוי',
          'עבדו עם עקומות, רמות, גוון וניגודיות, וטשטוש, רעש, עיוותים, טיפולים אמנותיים וכלי צילום.',
        ],
        [
          '↺',
          'ביטול בלי חשש',
          'כל מסמך שומר היסטוריה מלאה, והשכבות, הבחירה, הזום, שם הקובץ ומצב השינויים נשארים עצמאיים.',
        ],
      ],
    },
    screenshots: {
      eyebrow: 'הממשק האמיתי',
      title: 'סביבת עבודה שלא מפריעה.',
      lead: 'כל התמונות מגיעות מאותה ערכת צילומי Playwright קבועה שמגינה על הנאמנות החזותית של העורך.',
      rows: [
        [
          'עריכה ישירה',
          'טקסט עורכים על הבד.',
          'מקמו טקסט מרובה שורות, גררו אותו ושנו משפחה, גודל, משקל, יישור, מילוי, קו מתאר ורקע לפני האישור.',
          ['עריכה חיה על הבד', 'מילוי, קו מתאר ורקע', 'אישור במקלדת ובקרי טיפוגרפיה'],
          'טקסט מרובה שורות עם בקרי טיפוגרפיה חיים',
        ],
        [
          'בחירה מודעת לפיקסלים',
          'לבחור, לשלב, להזיז ולדייק.',
          'שלבו אזורים בהחלפה, איחוד, החרגה, XOR או חיתוך, ואז גזרו, העתיקו, הזיזו, חתכו או שנו אותם.',
          ['מלבן, אליפסה, לאסו, מצולע ומטה קסם', 'מצבי בחירה עם מקשי שינוי', 'אפקטים והתאמות בתוך הבחירה'],
          'הבחירות משתלבות בשכבות, בהיסטוריה ובאפקטים',
        ],
        [
          'ספריית אפקטים עשירה',
          'מתיקונים עדינים ועד שינויים פרועים.',
          'כוונו עקומות ורמות, תקנו תמונות, צרו מרקמים ופרקטלים או חקרו טשטוש, רעש, עיוות, רינדור וסגנון.',
          [
            '46 אפקטים מובנים ועוד 9 אופציונליים בתהליכי רקע',
            'חלונות פרמטרים עם תצוגה מקדימה',
            'היסטוריה עקבית לאחר כל הפעלה',
          ],
          'האפקטים מסודרים בקטגוריות המוכרות של Pinta',
        ],
      ],
      details: [
        ['עקומות', 'עקומה קובית טבעית לכל ערוץ'],
        ['עננים', 'רעש פרוצדורלי בשכבות'],
        ['ציור שמן', 'טיפול אמנותי מתכוונן'],
        ['קודם מקלדת', 'קיצורי Pinta גוברים על פעולות הדפדפן'],
      ],
    },
    formats: {
      eyebrow: 'פורמטים פתוחים, קבצים מקומיים',
      title: 'התמונות נשארות שימושיות — ונשארות שלכם.',
      lead: 'Pinta Online מעבד תמונות בדפדפן. פתחו כמה קבצים בכרטיסיות עצמאיות וייצאו לפורמט המתאים לשלב הבא.',
      open: 'פתיחה',
      save: 'שמירה',
      theme: 'ערכת הנושא הכהה והבהירה זוכרות את ההעדפה.',
    },
    local: {
      eyebrow: 'יישום רשת מתקדם ומקומי',
      title: ['פותחים כמו אתר.', 'משתמשים כמו אפליקציה.'],
      lead: 'התקינו את Pinta Online לחלון ייעודי ולהפעלה לא מקוונת. IndexedDB משחזר מקומית מסמכים, שכבות, פיקסלים, בחירות, זום וכרטיסיות.',
      points: [
        ['בלי חשבון', 'פותחים את העורך ומתחילים מיד.'],
        ['בלי העלאת תמונות', 'העריכה והאפקטים רצים בדפדפן.'],
        ['שחזור מלא', 'חוזרים למסמכים, לשכבות ולהיסטוריה.'],
        ['מוכן לעבודה לא מקוונת', 'הממשק והאיורים נשמרים מראש.'],
      ],
    },
    faq: {
      eyebrow: 'כדאי לדעת',
      title: 'שאלות ותשובות.',
      items: [
        ['האם Pinta Online חינמי?', 'כן. העורך ב־paint.rip חינמי ואינו דורש חשבון.'],
        [
          'האם התמונות נשלחות לשרת?',
          'לא. הפענוח, השילוב, השינוי והייצוא מתבצעים בדפדפן. שחזור סביבת העבודה נשמר מקומית במכשיר.',
        ],
        [
          'האם השכבות נשמרות?',
          'כן. ייבוא וייצוא OpenRaster שומרים שמות, נראות, אטימות, מצבי שילוב, סדר ונתוני פיקסלים.',
        ],
        ['האם קיצורי Pinta המקוריים עובדים?', 'כן. Ctrl/Command+N, O, S, W, R ומקשי הכלים המקוריים נתפסים בידי העורך.'],
        [
          'אפשר להתקין אותו?',
          'כן. דפדפן נתמך יכול להתקין את Pinta Online כיישום רשת מתקדם ולהפעיל את הממשק ללא חיבור.',
        ],
      ],
    },
    final: {
      eyebrow: 'לא צריך להתקין דבר מראש',
      title: 'הבד הבא נמצא במרחק לחיצה.',
      lead: 'פתחו את Pinta Online, לחצו Ctrl/Command+N וצרו משהו.',
      button: 'הפעלת Pinta Online',
    },
    footer: {
      description: 'עורך תמונות טבעי לדפדפן המבוסס על פרויקט הקוד הפתוח Pinta.',
      portedBy: 'הוסב לרשת על ידי',
      editor: 'עורך',
      source: 'קוד הרשת',
      project: 'פרויקט Pinta',
      issue: 'דיווח על בעיה',
      copyright: 'זכויות יוצרים © 2010–2026 תורמי Pinta. מופץ ברישיון MIT X11.',
    },
    featureList: [
      '23 כלי ציור ועריכה זמינים',
      'שכבות עם 16 מצבי שילוב',
      '55 התאמות ואפקטים מובנים ואופציונליים',
      'ייבוא וייצוא OpenRaster',
      'יישום רשת מתקדם לעבודה לא מקוונת',
    ],
    popart: {
      eyebrow: 'עולם אחד, עשרה עיבודים',
      title: 'יצירה אחת. תוצאות שונות לחלוטין.',
      intro:
        'פתחנו איור מקורי בשם Cosmic Garden ב־Pinta Online, ואז הוספנו טקסט חי, הילה שנוצרה בכלי הצורות וקו גרפי בשכבות נפרדות. לאחר מכן העברנו עותק משוטח דרך הספרייה, אפקט אחד בכל פעם. הנושא נשאר קבוע, כך שכל תמונה שונה רק בכלי שהיא מדגימה.',
      heroAlt: 'עורך Pinta Online עם סצנה סוריאליסטית של דג קוי, שער ירח ואסטרונאוט, וארבע שכבות בעלות שם בלוח השכבות.',
      heroCaption: 'איור מקורי ועוד שלוש שכבות Pinta ניתנות לעריכה: הילה, קו גרפי וטקסט חי',
      note: 'חמישה מהם מגיעים מכובים. פיקסול משושה, סטייה כרומטית, קווי סריקה, ארטיפקטים צבעוניים וראיית לילה מגיעים מתוספים מצורפים שמפעילים במנהל התוספים — כך נוצרה הרשת הזו, וכך תיצרו את שלכם.',
      untreated: 'ללא עיבוד',
      labels: [
        'איור מיובא, אליפסה, קו וטקסט חי',
        'פסיפס מתוסף',
        'קווי רומן גרפי',
        'ערוצים מוסטים ב־14 פיקסלים',
        'סריקת מסך',
        '128 מקטעים מחוללים',
        'שלוש רמות לכל ערוץ',
        'ההדפס בשלילה',
        'תגובה ירוקה עם גרעיניות',
        'גודל מכחול וגסות',
      ],
    },
  },
};

const localizedCodes = Object.keys(copy);
const allCodes = Object.keys(localeMeta);
const runtimeCodes = inventory.locales.map(({ code }) => code);
if (allCodes.join(',') !== SEO_LOCALE_CODES.join(',')) {
  throw new Error('SEO locale metadata must match SEO_LOCALE_CODES in scripts/i18n-config.mjs.');
}
const editorPath = (locale) => (locale === 'en' ? '/' : `/${locale}/`);
const aboutPath = (locale) => (locale === 'en' ? '/about/' : `/${locale}/about/`);
const analyticsTags = `    <meta name="google-tag-id" content="GT-TNLLJZ63" />
    <meta name="google-analytics-id" content="G-BZKV3EDF46" />
    <meta name="google-ads-id" content="AW-998871174" />
    <meta name="google-ads-page-view-conversion-id" content="AW-998871174/TDzECNTY5-ocEIahptwD" />
    <script type="module" src="/web-assets/analytics.js"></script>`;

function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;');
}

function alternateLinks(kind) {
  const pathFor = kind === 'editor' ? editorPath : aboutPath;
  return [
    ...allCodes.map((locale) => `    <link rel="alternate" hreflang="${locale}" href="${origin}${pathFor(locale)}" />`),
    `    <link rel="alternate" hreflang="x-default" href="${origin}${pathFor('en')}" />`,
  ].join('\n');
}

function openGraphLocales(locale) {
  return [
    `    <meta property="og:locale" content="${localeMeta[locale].ogLocale}" />`,
    ...allCodes
      .filter((code) => code !== locale)
      .map((code) => `    <meta property="og:locale:alternate" content="${localeMeta[code].ogLocale}" />`),
  ].join('\n');
}

function jsonLd(value) {
  return JSON.stringify(value, null, 2)
    .split('\n')
    .map((line) => `      ${line}`)
    .join('\n');
}

function editorPage(locale, text) {
  const canonical = `${origin}${editorPath(locale)}`;
  const graph = {
    '@context': 'https://schema.org',
    '@graph': [
      {
        '@type': 'WebPage',
        '@id': `${canonical}#page`,
        url: canonical,
        name: text.editorOgTitle,
        description: text.editorDescription,
        inLanguage: locale,
        isPartOf: { '@id': `${origin}/#website` },
        mainEntity: { '@id': `${origin}/#software` },
      },
      {
        '@type': 'SoftwareApplication',
        '@id': `${origin}/#software`,
        name: 'Pinta Online',
        alternateName: 'Paint.rip',
        url: canonical,
        applicationCategory: 'DesignApplication',
        operatingSystem: 'Any operating system with a modern web browser',
        browserRequirements: 'Requires JavaScript, HTML5 Canvas, and a modern browser',
        softwareVersion: '__PINTA_ONLINE_VERSION__',
        isAccessibleForFree: true,
        inLanguage: locale,
        image: `${origin}/about/assets/pinta-online-og.jpg`,
        screenshot: `${origin}/about/assets/editor-dark.webp`,
        description: text.editorDescription,
        offers: { '@type': 'Offer', price: '0', priceCurrency: 'USD' },
        featureList: text.featureList,
      },
    ],
  };
  return `<!doctype html>
<html lang="${locale}" dir="${localeMeta[locale].direction}">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="theme-color" content="#242424" />
    <meta name="robots" content="index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1" />
    <meta name="description" content="${escapeHtml(text.editorDescription)}" />
    <link rel="canonical" href="${canonical}" />
${alternateLinks('editor')}
    <link rel="icon" href="/apps/com.github.PintaProject.Pinta.svg" type="image/svg+xml" />
    <link rel="apple-touch-icon" href="/icons/pinta-192.png" />
${analyticsTags}

    <meta property="og:type" content="website" />
    <meta property="og:site_name" content="Pinta Online" />
    <meta property="og:title" content="${escapeHtml(text.editorOgTitle)}" />
    <meta property="og:description" content="${escapeHtml(text.editorOgDescription)}" />
    <meta property="og:url" content="${canonical}" />
${openGraphLocales(locale)}
    <meta property="og:image" content="${origin}/about/assets/pinta-online-og.jpg" />
    <meta property="og:image:width" content="1200" />
    <meta property="og:image:height" content="630" />
    <meta property="og:image:alt" content="${escapeHtml(text.editorImageAlt)}" />

    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:title" content="${escapeHtml(text.editorOgTitle)}" />
    <meta name="twitter:description" content="${escapeHtml(text.editorOgDescription)}" />
    <meta name="twitter:image" content="${origin}/about/assets/pinta-online-og.jpg" />
    <meta name="twitter:image:alt" content="${escapeHtml(text.editorImageAlt)}" />

    <script type="application/ld+json">
${jsonLd(graph)}
    </script>
    <title>${escapeHtml(text.editorTitle)}</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
`;
}

// High-coverage Pinta catalogs can ship in the editor before the much larger,
// web-specific About copy has a reviewed translation. These route shells boot
// the localized app but deliberately stay out of search indexes and hreflang
// clusters until their SEO content is genuinely localized.
function editorLocaleShell(locale) {
  const metadata = runtimeLocaleMeta[locale];
  return `<!doctype html>
<html lang="${locale}" dir="${metadata.direction}">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover" />
    <meta name="theme-color" content="#242424" />
    <meta name="robots" content="noindex, follow" />
    <link rel="canonical" href="${origin}/" />
    <link rel="icon" href="/apps/com.github.PintaProject.Pinta.svg" type="image/svg+xml" />
    <link rel="apple-touch-icon" href="/icons/pinta-192.png" />
${analyticsTags}
    <title>Pinta Online — ${escapeHtml(metadata.name)}</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
`;
}

function languageSwitcher(locale, kind, label) {
  const pathFor = kind === 'editor' ? editorPath : aboutPath;
  return `<details class="language-switcher">
        <summary aria-label="${escapeHtml(label)}"><span aria-hidden="true">◎</span> ${escapeHtml(localeMeta[locale].name)}</summary>
        <div class="language-menu">
${allCodes.map((code) => `          <a href="${pathFor(code)}" lang="${code}" dir="${localeMeta[code].direction}" hreflang="${code}"${code === locale ? ' aria-current="page"' : ''}>${escapeHtml(localeMeta[code].name)}</a>`).join('\n')}
        </div>
      </details>`;
}

function footerLanguageLinks(locale, label) {
  return `<div class="footer-languages">
        <span>${escapeHtml(label)}</span>
        <nav aria-label="${escapeHtml(label)}">
${allCodes.map((code) => `          <a href="${editorPath(code)}" lang="${code}" dir="${localeMeta[code].direction}" hreflang="${code}"${code === locale ? ' aria-current="true"' : ''}>${escapeHtml(localeMeta[code].name)}</a>`).join('\n')}
        </nav>
      </div>`;
}

function aboutPage(locale, text) {
  const canonical = `${origin}${aboutPath(locale)}`;
  const editorUrl = `${origin}${editorPath(locale)}`;
  const images = [
    ['text-editor.webp', text.screenshots.rows[0]],
    ['selections.webp', text.screenshots.rows[1]],
    ['effects-library.webp', text.screenshots.rows[2]],
  ];
  const details = [
    ['curves.webp', 520, 576],
    ['clouds.webp', 520, 414],
    ['oil-painting.webp', 520, 280],
    ['keyboard-shortcuts.webp', 760, 720],
  ];
  const graph = {
    '@context': 'https://schema.org',
    '@graph': [
      {
        '@type': 'WebPage',
        '@id': `${canonical}#page`,
        url: canonical,
        name: text.aboutTitle.replace(' | Paint.rip', ''),
        description: text.aboutDescription,
        inLanguage: locale,
        isPartOf: { '@id': `${origin}/#website` },
        mainEntity: { '@id': `${origin}/#software` },
        primaryImageOfPage: { '@id': `${canonical}#hero-image` },
      },
      {
        '@type': 'ImageObject',
        '@id': `${canonical}#hero-image`,
        url: `${origin}/about/assets/pinta-online-og.jpg`,
        width: 1200,
        height: 630,
        caption: text.editorImageAlt,
      },
      {
        '@type': 'BreadcrumbList',
        itemListElement: [
          { '@type': 'ListItem', position: 1, name: text.footer.editor, item: editorUrl },
          { '@type': 'ListItem', position: 2, name: text.nav[0], item: canonical },
        ],
      },
      {
        '@type': 'SoftwareApplication',
        '@id': `${origin}/#software`,
        name: 'Pinta Online',
        url: editorUrl,
        applicationCategory: 'DesignApplication',
        operatingSystem: 'Any operating system with a modern web browser',
        browserRequirements: 'Requires JavaScript, HTML5 Canvas, and a modern browser',
        softwareVersion: '__PINTA_ONLINE_VERSION__',
        isAccessibleForFree: true,
        inLanguage: locale,
        image: `${origin}/about/assets/pinta-online-og.jpg`,
        screenshot: [
          `${origin}/about/assets/editor-dark.webp`,
          `${origin}/about/assets/text-editor.webp`,
          `${origin}/about/assets/effects-library.webp`,
        ],
        description: text.aboutDescription,
        offers: { '@type': 'Offer', price: '0', priceCurrency: 'USD' },
        featureList: text.featureList,
      },
    ],
  };
  return `<!doctype html>
<html lang="${locale}" dir="${localeMeta[locale].direction}">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="theme-color" content="#111117" />
    <meta name="color-scheme" content="dark" />
    <meta name="robots" content="index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1" />
    <meta name="description" content="${escapeHtml(text.aboutDescription)}" />
    <title>${escapeHtml(text.aboutTitle)}</title>
    <link rel="canonical" href="${canonical}" />
${alternateLinks('about')}
    <link rel="icon" href="/apps/com.github.PintaProject.Pinta.svg" type="image/svg+xml" />
    <link rel="apple-touch-icon" href="/icons/pinta-192.png" />
${analyticsTags}

    <meta property="og:type" content="website" />
    <meta property="og:site_name" content="Pinta Online" />
    <meta property="og:title" content="${escapeHtml(text.aboutOgTitle)}" />
    <meta property="og:description" content="${escapeHtml(text.aboutOgDescription)}" />
    <meta property="og:url" content="${canonical}" />
${openGraphLocales(locale)}
    <meta property="og:image" content="${origin}/about/assets/pinta-online-og.jpg" />
    <meta property="og:image:width" content="1200" />
    <meta property="og:image:height" content="630" />
    <meta property="og:image:alt" content="${escapeHtml(text.editorImageAlt)}" />
    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:title" content="${escapeHtml(text.aboutOgTitle)}" />
    <meta name="twitter:description" content="${escapeHtml(text.aboutOgDescription)}" />
    <meta name="twitter:image" content="${origin}/about/assets/pinta-online-og.jpg" />
    <meta name="twitter:image:alt" content="${escapeHtml(text.editorImageAlt)}" />
    <script type="application/ld+json">
${jsonLd(graph)}
    </script>
    <link rel="stylesheet" href="/about/about.css" />
  </head>
  <body>
    <a class="skip-link" href="#main">${escapeHtml(text.skip)}</a>
    <header class="site-header">
      <a class="brand" href="${editorPath(locale)}" aria-label="${escapeHtml(text.openEditor)}">
        <img src="/apps/com.github.PintaProject.Pinta.svg" width="40" height="40" alt="" />
        <span><strong>Pinta</strong> Online</span>
      </a>
      <nav aria-label="${escapeHtml(text.nav.join(', '))}">
        <a href="/user-guide/">User Guide</a><a href="#features">${escapeHtml(text.nav[0])}</a><a href="#screenshots">${escapeHtml(text.nav[1])}</a><a href="#formats">${escapeHtml(text.nav[2])}</a><a href="#questions">${escapeHtml(text.nav[3])}</a>
      </nav>
      ${languageSwitcher(locale, 'about', text.languageLabel)}
      <a class="button button-small" href="${editorPath(locale)}">${escapeHtml(text.openEditor)} <span aria-hidden="true">↗</span></a>
    </header>

    <main id="main">
      <section class="hero" aria-labelledby="hero-title">
        <div class="hero-copy">
          <p class="eyebrow"><span></span>${escapeHtml(text.hero.eyebrow)}</p>
          <h1 id="hero-title">${escapeHtml(text.hero.title[0])}<br /><em>${escapeHtml(text.hero.title[1])}</em></h1>
          <p class="hero-lede">${escapeHtml(text.hero.lead)}</p>
          <div class="hero-actions"><a class="button button-primary" href="${editorPath(locale)}">${escapeHtml(text.hero.start)} <span aria-hidden="true">→</span></a><a class="button button-quiet" href="#screenshots">${escapeHtml(text.hero.see)}</a></div>
          <ul class="trust-list">${text.hero.trust.map((item) => `<li><span aria-hidden="true">✓</span>${escapeHtml(item)}</li>`).join('')}</ul>
        </div>
        <figure class="hero-visual">
          <div class="window-dots" aria-hidden="true"><i></i><i></i><i></i></div>
          <img src="/about/assets/editor-dark.webp" width="1200" height="800" alt="${escapeHtml(text.editorImageAlt)}" fetchpriority="high" />
          <figcaption>${escapeHtml(text.hero.caption)}</figcaption><span class="floating-chip chip-tools">23</span><span class="floating-chip chip-effects">55</span>
        </figure>
      </section>
      <section class="numbers" aria-label="${escapeHtml(text.nav[0])}">${[23, 16, 55, 12].map((number, index) => `<div><strong>${number}</strong><span>${escapeHtml(text.stats[index])}</span></div>`).join('')}</section>

      <section class="section" id="features" aria-labelledby="features-title">
        <div class="section-heading"><p class="eyebrow"><span></span>${escapeHtml(text.features.eyebrow)}</p><h2 id="features-title">${escapeHtml(text.features.title)}</h2><p>${escapeHtml(text.features.lead)}</p></div>
        <div class="feature-grid">${text.features.cards.map(([icon, title, description]) => `<article class="feature-card"><span class="feature-icon" aria-hidden="true">${icon}</span><h3>${escapeHtml(title)}</h3><p>${escapeHtml(description)}</p></article>`).join('')}</div>
      </section>

      <section class="showcase section" id="screenshots" aria-labelledby="screenshots-title">
        <div class="section-heading section-heading-left"><p class="eyebrow"><span></span>${escapeHtml(text.screenshots.eyebrow)}</p><h2 id="screenshots-title">${escapeHtml(text.screenshots.title)}</h2><p>${escapeHtml(text.screenshots.lead)}</p></div>
        ${images.map(([file, row], index) => `<article class="showcase-row${index === 1 ? ' showcase-row-reverse' : ''}"><div class="showcase-copy"><p class="overline">${escapeHtml(row[0])}</p><h3>${escapeHtml(row[1])}</h3><p>${escapeHtml(row[2])}</p><ul class="check-list">${row[3].map((item) => `<li>${escapeHtml(item)}</li>`).join('')}</ul></div><figure class="screenshot-frame ${index === 1 ? 'tilt-left' : 'tilt-right'}"><img src="/about/assets/${file}" width="960" height="640" loading="lazy" alt="${escapeHtml(row[4])}" /><figcaption>${escapeHtml(row[4])}</figcaption></figure></article>`).join('\n        ')}
        <div class="detail-gallery" aria-label="${escapeHtml(text.nav[1])}">
          <figure class="detail-card detail-tall"><img src="/about/assets/${details[0][0]}" width="${details[0][1]}" height="${details[0][2]}" loading="lazy" alt="${escapeHtml(text.screenshots.details[0][0])}" /><figcaption><strong>${escapeHtml(text.screenshots.details[0][0])}</strong><span>${escapeHtml(text.screenshots.details[0][1])}</span></figcaption></figure>
          <div class="detail-stack">
            ${[1, 2].map((index) => `<figure class="detail-card"><img src="/about/assets/${details[index][0]}" width="${details[index][1]}" height="${details[index][2]}" loading="lazy" alt="${escapeHtml(text.screenshots.details[index][0])}" /><figcaption><strong>${escapeHtml(text.screenshots.details[index][0])}</strong><span>${escapeHtml(text.screenshots.details[index][1])}</span></figcaption></figure>`).join('')}
          </div>
          <figure class="detail-card detail-tall"><img src="/about/assets/${details[3][0]}" width="${details[3][1]}" height="${details[3][2]}" loading="lazy" alt="${escapeHtml(text.screenshots.details[3][0])}" /><figcaption><strong>${escapeHtml(text.screenshots.details[3][0])}</strong><span>${escapeHtml(text.screenshots.details[3][1])}</span></figcaption></figure>
        </div>
      </section>

      <section class="popart section" id="popart" aria-labelledby="popart-title">
        <div class="section-heading section-heading-left">
          <p class="eyebrow"><span></span>${escapeHtml(text.popart.eyebrow)}</p>
          <h2 id="popart-title">${escapeHtml(text.popart.title)}</h2>
          <p>${escapeHtml(text.popart.intro)}</p>
        </div>
        <figure class="popart-hero"><img src="/about/assets/pop-workspace.webp" width="1280" height="720" loading="lazy" alt="${escapeHtml(text.popart.heroAlt)}" /><figcaption>${escapeHtml(text.popart.heroCaption)}</figcaption></figure>
        <ul class="popart-grid" aria-label="${escapeHtml(text.popart.title)}">
          ${popartPlates(locale)
            .map(
              ([file, name], index) =>
                `<li class="popart-cell${index === 0 ? ' popart-plate' : ''}"><img src="/about/assets/${file}" width="657" height="492" loading="lazy" alt="${escapeHtml(name)}" /><span class="popart-label"><b>${escapeHtml(name)}</b>${escapeHtml(text.popart.labels[index])}</span></li>`,
            )
            .join('\n          ')}
        </ul>
        <p class="popart-note">${escapeHtml(text.popart.note)}</p>
      </section>
      <section class="split-section section" id="formats" aria-labelledby="formats-title">
        <div><p class="eyebrow"><span></span>${escapeHtml(text.formats.eyebrow)}</p><h2 id="formats-title">${escapeHtml(text.formats.title)}</h2><p>${escapeHtml(text.formats.lead)}</p><div class="format-groups"><div><strong>${escapeHtml(text.formats.open)}</strong><ul class="format-list"><li>OpenRaster</li><li>PNG</li><li>JPEG</li><li>WebP</li><li>AVIF</li><li>GIF</li><li>BMP</li><li>TIFF</li><li>SVG</li><li>ICO</li><li>PPM</li><li>TGA</li></ul></div><div><strong>${escapeHtml(text.formats.save)}</strong><ul class="format-list"><li>OpenRaster</li><li>PNG</li><li>JPEG</li><li>WebP</li><li>BMP</li><li>TIFF</li><li>PPM</li><li>TGA</li></ul></div></div></div>
        <figure class="light-preview"><img src="/about/assets/editor-light.webp" width="960" height="640" loading="lazy" alt="${escapeHtml(text.formats.theme)}" /><figcaption>${escapeHtml(text.formats.theme)}</figcaption></figure>
      </section>

      <section class="local-first section" aria-labelledby="local-title"><div class="local-orb" aria-hidden="true"><span>⌁</span></div><div><p class="eyebrow"><span></span>${escapeHtml(text.local.eyebrow)}</p><h2 id="local-title">${escapeHtml(text.local.title[0])}<br />${escapeHtml(text.local.title[1])}</h2><p>${escapeHtml(text.local.lead)}</p></div><ul class="local-points">${text.local.points.map(([title, description]) => `<li><strong>${escapeHtml(title)}</strong><span>${escapeHtml(description)}</span></li>`).join('')}</ul></section>

      <section class="questions section" id="questions" aria-labelledby="questions-title"><div class="section-heading section-heading-left"><p class="eyebrow"><span></span>${escapeHtml(text.faq.eyebrow)}</p><h2 id="questions-title">${escapeHtml(text.faq.title)}</h2></div><div class="faq-list">${text.faq.items.map(([question, answer], index) => `<details${index === 0 ? ' open' : ''}><summary>${escapeHtml(question)}</summary><p>${escapeHtml(answer)}</p></details>`).join('')}</div></section>
      <section class="final-cta" aria-labelledby="cta-title"><img src="/apps/com.github.PintaProject.Pinta.svg" width="96" height="96" alt="" /><p class="eyebrow"><span></span>${escapeHtml(text.final.eyebrow)}</p><h2 id="cta-title">${escapeHtml(text.final.title)}</h2><p>${escapeHtml(text.final.lead)}</p><a class="button button-primary" href="${editorPath(locale)}">${escapeHtml(text.final.button)} <span aria-hidden="true">→</span></a></section>
    </main>

    <footer class="site-footer"><a class="brand" href="${editorPath(locale)}"><img src="/apps/com.github.PintaProject.Pinta.svg" width="34" height="34" alt="" /><span><strong>Pinta</strong> Online</span></a><p>${escapeHtml(text.footer.description)} ${escapeHtml(text.footer.portedBy)} <a href="https://github.com/evgenyvinnik/pinta-online">Evgeny Vinnik</a>.</p><nav aria-label="Footer navigation"><a href="${editorPath(locale)}">${escapeHtml(text.footer.editor)}</a><a href="${aboutPath(locale)}" aria-current="page">${escapeHtml(text.nav[0])}</a><a href="/promo/">Quick designs</a><a href="/user-guide/">User Guide</a><a href="https://github.com/evgenyvinnik/pinta-online">${escapeHtml(text.footer.source)}</a><a href="https://www.pinta-project.com">${escapeHtml(text.footer.project)}</a><a href="https://github.com/evgenyvinnik/pinta-online/issues/new?template=bug.md">${escapeHtml(text.footer.issue)}</a></nav>${footerLanguageLinks(locale, text.languageLabel)}<small><span>Pinta Online <strong data-app-version>__PINTA_ONLINE_VERSION__</strong></span><span>${escapeHtml(text.footer.copyright)}</span></small></footer>
  </body>
</html>
`;
}

function sitemap() {
  const entries = [
    ...allCodes.map((locale) => ({ path: editorPath(locale), kind: 'editor' })),
    ...allCodes.map((locale) => ({ path: aboutPath(locale), kind: 'about' })),
    { path: '/promo/' },
    { path: '/user-guide/' },
  ];
  const alternateElements = (kind) => {
    const pathFor = kind === 'editor' ? editorPath : aboutPath;
    return [
      ...allCodes.map(
        (locale) => `    <xhtml:link rel="alternate" hreflang="${locale}" href="${origin}${pathFor(locale)}" />`,
      ),
      `    <xhtml:link rel="alternate" hreflang="x-default" href="${origin}${pathFor('en')}" />`,
    ].join('\n');
  };
  const urlElement = ({ path, kind }) =>
    ['  <url>', `    <loc>${origin}${path}</loc>`, kind ? alternateElements(kind) : '', '  </url>']
      .filter(Boolean)
      .join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"
        xmlns:xhtml="http://www.w3.org/1999/xhtml">
${entries.map(urlElement).join('\n')}
</urlset>
`;
}

/**
 * The pop-art plates, paired with the effect name in the target language.
 *
 * The names are read from the generated runtime catalogs rather than restated in the copy blocks
 * above: they are the same strings the menus show, so a label here can never drift from what the
 * reader will actually find in the Effects menu. The first plate is untreated and has no effect
 * name, so it takes the locale's own word for it from the label list.
 */
function popartPlates(locale) {
  const catalog = JSON.parse(readFileSync(resolve(root, `src/i18n/locales/${locale}.json`), 'utf8'));
  const name = (english) => catalog[english] ?? english;
  return [
    ['pop-original.webp', copy[locale].popart.untreated],
    ['pop-halftone.webp', name('Hexagon Pixelate')],
    ['pop-inksketch.webp', name('Ink Sketch')],
    ['pop-aberration.webp', name('Chromatic Aberration')],
    ['pop-scanlines.webp', name('Scanlines')],
    ['pop-artifacts.webp', name('Colored Artifacts')],
    ['pop-posterize.webp', name('Posterize')],
    ['pop-invert.webp', name('Invert Colors')],
    ['pop-nightvision.webp', name('Night Vision')],
    ['pop-oilpaint.webp', name('Oil Painting')],
  ];
}

const outputs = new Map([[resolve(root, 'web-assets/seo/sitemap.xml'), sitemap()]]);
for (const locale of localizedCodes) {
  outputs.set(resolve(root, locale, 'index.html'), editorPage(locale, copy[locale]));
  outputs.set(resolve(root, locale, 'about/index.html'), aboutPage(locale, copy[locale]));
}
for (const locale of runtimeCodes.filter((code) => code !== 'en' && !localizedCodes.includes(code))) {
  outputs.set(resolve(root, locale, 'index.html'), editorLocaleShell(locale));
}

let staleFiles = 0;
for (const [path, content] of outputs) {
  if (checkOnly) {
    if (!existsSync(path) || readFileSync(path, 'utf8') !== content) {
      staleFiles += 1;
      console.error(`${path.slice(root.length + 1)} is stale; run npm run seo:sync`);
    }
    continue;
  }
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, content);
  console.log(`Generated ${path.slice(root.length + 1)}`);
}

if (staleFiles) process.exit(1);
if (checkOnly) console.log('Localized SEO pages and sitemap are synchronized.');
