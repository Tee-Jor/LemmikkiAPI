# LemmikkiAPI

docker build -t lemmikkiapi .

docker run -d -p 5000:5000 lemmikkiapi

jos halutaan säilyttää Data.db niin käynnistetään näin docker run -d -p 5000:5000 -v $(pwd)/data:/app lemmikkiapi
