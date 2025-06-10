#!/usr/bin/env python3
"""
Hacker News Scraper Robot
Searches Hacker News for mentions matching a regex pattern
"""

import eywa
import asyncio
import re
from datetime import datetime
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from webdriver_manager.chrome import ChromeDriverManager
import time


def setup_driver(headless=False):
    """Setup Chrome driver with optimal settings"""
    chrome_options = Options()

    # Only add headless if requested
    if headless:
        chrome_options.add_argument('--headless')
        eywa.info("Running in headless mode")
    else:
        eywa.info("Running with visible browser window")

    # Common options for both modes
    chrome_options.add_argument('--no-sandbox')
    chrome_options.add_argument('--disable-dev-shm-usage')
    chrome_options.add_argument('--disable-gpu')
    chrome_options.add_argument('--window-size=1920,1080')

    # Additional options for better visibility when not headless
    if not headless:
        chrome_options.add_argument('--start-maximized')

    # Auto-install ChromeDriver
    eywa.info("Setting up ChromeDriver...")
    service = Service(ChromeDriverManager().install())
    driver = webdriver.Chrome(service=service, options=chrome_options)

    return driver


async def search_hn_posts(driver, search_term, regex_pattern, max_results=10):
    """Search HN posts and comments for regex matches"""
    eywa.info(f"Searching Hacker News for pattern: {regex_pattern}")

    matches = []
    page = 1

    # Compile regex
    pattern = re.compile(regex_pattern, re.IGNORECASE)

    while len(matches) < max_results and page <= 5:  # Max 5 pages
        # Use HN search via Algolia
        search_url = f"https://hn.algolia.com/?query={
            search_term}&sort=byDate&page={page-1}"
        driver.get(search_url)

        # Wait for results to load
        wait = WebDriverWait(driver, 10)
        try:
            wait.until(EC.presence_of_element_located(
                (By.CLASS_NAME, "Story")))
        except:
            eywa.warn("No more results found")
            break

        # Get all story items
        stories = driver.find_elements(By.CLASS_NAME, "Story")

        for story in stories:
            try:
                # Get story details
                title_elem = story.find_element(By.CLASS_NAME, "Story_title")
                link_elem = title_elem.find_element(By.TAG_NAME, "a")
                title = link_elem.text
                url = link_elem.get_attribute("href")

                # Get metadata
                meta = story.find_element(By.CLASS_NAME, "Story_meta")
                meta_text = meta.text

                # Extract points, comments, time
                points_match = re.search(r'(\d+) points?', meta_text)
                comments_match = re.search(r'(\d+) comments?', meta_text)
                time_match = re.search(r'(\d+ \w+ ago)', meta_text)

                points = int(points_match.group(1)) if points_match else 0
                comments = int(comments_match.group(1)
                               ) if comments_match else 0
                time_ago = time_match.group(1) if time_match else "unknown"

                # Check if title matches pattern
                if pattern.search(title):
                    # Get HN discussion link
                    hn_link = story.find_element(By.PARTIAL_LINK_TEXT, "comment").get_attribute(
                        "href") if comments > 0 else None

                    match = {
                        'title': title,
                        'url': url,
                        'hn_url': hn_link,
                        'points': points,
                        'comments': comments,
                        'time': time_ago,
                        'match_type': 'title',
                        'matched_text': pattern.search(title).group(0)
                    }

                    matches.append(match)
                    eywa.info(f"Found match #{len(matches)}: {title}")

                    if len(matches) >= max_results:
                        break

            except Exception as e:
                continue

        page += 1
        time.sleep(1)  # Be nice to the server

    return matches


async def scrape_hn_comments(driver, story_url, regex_pattern, story_title):
    """Scrape comments from a specific HN story"""
    eywa.info(f"Checking comments for: {story_title}")

    driver.get(story_url)
    pattern = re.compile(regex_pattern, re.IGNORECASE)
    comment_matches = []

    # Wait for comments to load
    wait = WebDriverWait(driver, 10)
    try:
        wait.until(EC.presence_of_element_located((By.CLASS_NAME, "comment")))
    except:
        return comment_matches

    # Get all comments
    comments = driver.find_elements(By.CLASS_NAME, "comment")

    for comment in comments[:50]:  # Check first 50 comments
        try:
            text = comment.text
            if pattern.search(text):
                # Get comment metadata
                meta = comment.find_element(By.CLASS_NAME, "comhead")
                user = meta.find_element(By.CLASS_NAME, "hnuser").text if meta.find_elements(
                    By.CLASS_NAME, "hnuser") else "unknown"

                comment_matches.append({
                    'story_title': story_title,
                    'story_url': story_url,
                    'comment_text': text[:200] + "..." if len(text) > 200 else text,
                    'user': user,
                    'matched_text': pattern.search(text).group(0)
                })

        except:
            continue

    return comment_matches


async def main():
    """Main robot entry point"""
    eywa.open_pipe()
    driver = None

    try:
        # Get task input
        task = await eywa.get_task()
        input_data = task.get('data', {})

        # Get parameters
        search_term = input_data.get('search_term', 'EYWA')
        regex_pattern = input_data.get('regex_pattern', r'EYWA|eywa')
        max_results = input_data.get('max_results', 10)
        check_comments = input_data.get('check_comments', False)
        # Default to visible browser
        headless = input_data.get('headless', False)

        eywa.update_task(eywa.PROCESSING)

        # Setup driver with headless option
        driver = setup_driver(headless=headless)

        # Add a small delay for non-headless mode so user can see what's happening
        if not headless:
            eywa.info("Browser window opened - starting search in 2 seconds...")
            time.sleep(2)

        # Search posts
        post_matches = await search_hn_posts(driver, search_term, regex_pattern, max_results)

        # Optional: Check comments
        comment_matches = []
        if check_comments and post_matches:
            eywa.info("Checking comments for matches...")
            for match in post_matches[:5]:  # Check comments for top 5 posts
                if match['hn_url'] and match['comments'] > 0:
                    comments = await scrape_hn_comments(
                        driver,
                        match['hn_url'],
                        regex_pattern,
                        match['title']
                    )
                    comment_matches.extend(comments)

        # Prepare report
        report = {
            'search_term': search_term,
            'regex_pattern': regex_pattern,
            'post_matches': post_matches,
            'comment_matches': comment_matches,
            'total_matches': len(post_matches) + len(comment_matches),
            'scraped_at': datetime.now().isoformat(),
            'mode': 'headless' if headless else 'visible'
        }

        # Store in EYWA
        # await eywa.graphql("""
        #     mutation($data: JSON) {
        #         createHNScrapeReport(data: {
        #             search_term: $search_term,
        #             pattern: $pattern,
        #             results: $data,
        #             match_count: $count
        #         }) { euuid }
        #     }
        # """, {
        #     'search_term': search_term,
        #     'pattern': regex_pattern,
        #     'data': report,
        #     'count': report['total_matches']
        # })
        #
        eywa.report("HN scraping complete", report)

        # Keep browser open for a few seconds in non-headless mode
        if not headless:
            eywa.info("Keeping browser open for 5 seconds for review...")
            time.sleep(5)

        eywa.close_task(eywa.SUCCESS)

    except Exception as e:
        eywa.error(f"Robot failed: {str(e)}")
        eywa.close_task(eywa.ERROR)
    finally:
        if driver:
            eywa.info("Closing browser...")
            driver.quit()


if __name__ == "__main__":
    asyncio.run(main())
