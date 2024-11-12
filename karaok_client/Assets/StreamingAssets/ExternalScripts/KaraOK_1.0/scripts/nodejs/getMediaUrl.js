const puppeteer = require('puppeteer');
const fs = require('fs').promises;
const path = require('path');

const cookiesPath = path.resolve(__dirname, 'cookies.json');

// Load cookies from file if they exist
async function loadCookies(page) {
    try {
        const cookiesString = await fs.readFile(cookiesPath, 'utf8');
        const cookies = JSON.parse(cookiesString);
        await page.setCookie(...cookies);
        console.log("Loaded cookies from file.");
    } catch (error) {
        console.log("No cookies found.");
    }
}

// Save cookies to file
async function saveCookies(page) {
    const cookies = await page.cookies();
    await fs.writeFile(cookiesPath, JSON.stringify(cookies, null, 2));
    console.log("Cookies saved successfully.");
}

// Prompt user to log in and save cookies for future sessions
async function promptLogin(url) {
    const browser = await puppeteer.launch({ headless: false });
    const page = await browser.newPage();

    // Go to the playlist URL to allow login
    await page.goto(url, { waitUntil: 'networkidle2' });
    console.log("Please log in to Smule. Cookies will be saved for future sessions.");
    await page.setDefaultNavigationTimeout(360000);

    // Wait for user to log in and any page navigation that might happen post-login
    await page.waitForNavigation({ waitUntil: 'networkidle2' });

    // Save cookies after successful login
    await saveCookies(page);

    await browser.close();
}

async function processApiResponse(page, response) {
    let allPlaylistItems = [];
    const requestUrl = response.request().url();

    if (requestUrl.includes('api/playlists/aplist/view')) {
        if (response.status() === 403) {
            console.log("Access forbidden. Login required.");
            return { forbidden: true };
        }

        try {
            // Use `response.text()` and decode it as UTF-8
            const textResponse = await response.text();
            console.log("Raw text response:", textResponse);  // Log the raw response for debugging

            // Parse the text as JSON
            const jsonResponse = JSON.parse(textResponse);

            // Extract only artist, title, and web_url from each playlist item
            const playlistItems = (jsonResponse.playlistItems.items || []).map(item => ({
                artist: item.performance.artist || "Unknown Artist",
                title: item.performance.title || "Untitled",
                web_url: GetCorrectWebUrl(item.performance.web_url)
            }));

            allPlaylistItems = allPlaylistItems.concat(playlistItems);
            console.log("Processed Playlist Items:", JSON.stringify(allPlaylistItems));

            let nextCursor = jsonResponse.playlistItems.cursor?.next || null;

            while (nextCursor) {
                const paginatedUrl = requestUrl.replace("cursor=start", `cursor=${nextCursor}`);
                const newResponse = await page.goto(paginatedUrl, { waitUntil: 'networkidle2' });

                if (newResponse.status() === 403) {
                    console.log("Access forbidden. Login required.");
                    return { forbidden: true };
                }

                const textResponse = await newResponse.text();
                const jsonResponse = JSON.parse(textResponse);

                const playlistItems = (jsonResponse.playlistItems.items || []).map(item => ({
                    artist: item.performance.artist || "Unknown Artist",
                    title: item.performance.title || "Untitled",
                    web_url: GetCorrectWebUrl(item.performance.web_url)
                }));

                allPlaylistItems = allPlaylistItems.concat(playlistItems);
                nextCursor = jsonResponse.playlistItems.cursor?.next || null;
            }

            return { items: allPlaylistItems, forbidden: false };
        } catch (error) {
            console.error("Error parsing JSON:", error);
            return { items: [], forbidden: false };
        }
    }

    return { forbidden: false };
}

// Adds the correct base URL to the extracted URL path
function GetCorrectWebUrl(extractedUrl) {
    return `https://www.smule.com${extractedUrl || ""}`;
}

async function getMediaUrl(url) {
    const browser = await puppeteer.launch({ headless: true });
    const page = await browser.newPage();

    let mediaUrls = new Set();
    let artist = null;
    let title = null;

    // Listen for network requests to capture media URLs
    page.on('response', async (response) => {
        const request = response.request();

        // Check if the request is an `xhr` and contains `.ts` files
        if (request.resourceType() === 'xhr' && /\.ts$/i.test(request.url())) {
            mediaUrls.add(request.url());
        }
        // Check for other media files in `media` requests
        else if (request.resourceType() === 'media' && /(.mp4|.m4a|.aac|.mp3)$/i.test(request.url())) {
            mediaUrls.add(request.url());
        }
    });

    // Navigate to the URL and wait until the page is fully loaded
    await page.goto(url, { waitUntil: 'networkidle2' });

    // Wait for the DataStore object to be available and extract artist and title
    const dataStore = await page.evaluate(() => window.DataStore || null);

    if (dataStore && dataStore.Pages && dataStore.Pages.Recording && dataStore.Pages.Recording.performance) {
        artist = dataStore.Pages.Recording.performance.artist || null;
        title = dataStore.Pages.Recording.performance.title || null;
    } else {
        console.log("DataStore object not found or does not contain artist/title information.");
    }

    await browser.close();

    // Format the data into a list of unique objects
    const result = Array.from(mediaUrls).map(url => ({
        mediaUrl: url,
        artist,
        title
    }));

    // Output the JSON array as the last output to be captured by C#
    console.log(JSON.stringify(result));
}

// Fetch playlist items from Smule, handling forbidden response
async function getPlaylistItems(url) {
    const browser = await puppeteer.launch({ headless: true });
    const page = await browser.newPage();
    await page.setExtraHTTPHeaders({
        'Accept-Charset': 'utf-8'
    });
    // Load cookies for the session
    await loadCookies(page);

    let playlistItems = {};
    let forbiddenEncountered = false;

    // Listen for responses and process the API response
    page.on('response', async (response) => {
        var requestUrl = response.request().url();
        if (requestUrl.includes("api/playlists/aplist/view")) {
            const result = await processApiResponse(page, response);
            if (result.forbidden) {
                forbiddenEncountered = true;
            } else if (result.items) {
                playlistItems.playlist = result.items;
            }
        }
    });
    // Navigate to the playlist URL
    await page.goto(url, { waitUntil: 'networkidle2' });
    await browser.close();

    // If forbidden, prompt for login and retry fetching items
    if (forbiddenEncountered) {
        await promptLogin(url);
        return await getPlaylistItems(url); // Retry after login
    }

    console.log(JSON.stringify(playlistItems));
}

// Determine which function to call based on URL pattern
async function main(url) {
    const recordingPattern = /^https:\/\/www\.smule\.com\/recording\/[^/]+\/\d+/;
    const playlistPattern = /^https:\/\/www\.smule\.com\/playlist\/\d+/;

    if (recordingPattern.test(url)) {
        await getMediaUrl(url);
    } else if (playlistPattern.test(url)) {
        await getPlaylistItems(url);
    } else {
        console.log("Invalid URL format. Please provide a recording or playlist URL.");
    }
}

// Run the main function with the provided URL argument
const url = process.argv[2];
main(url);