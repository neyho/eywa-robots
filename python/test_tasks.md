# Hacker News Scraper Test Examples

## 1. Visible Browser Mode (Default)
```json
{
  "input": {
    "search_term": "Python",
    "regex_pattern": "Python|python|Py",
    "max_results": 5,
    "check_comments": false,
    "headless": false
  }
}
```
Run: `eywa run --task-file visible_test.json -c "cd python" -c ".venv/bin/python hn_scraper.py"`

## 2. Headless Mode (Background)
```json
{
  "input": {
    "search_term": "robotics",
    "regex_pattern": "robot|robotic|RPA",
    "max_results": 10,
    "check_comments": true,
    "headless": true
  }
}
```
Run: `eywa run --task-file headless_test.json -c "cd python" -c ".venv/bin/python hn_scraper.py"`

## 3. Interactive Mode (Watch the scraping)
```json
{
  "input": {
    "search_term": "AI",
    "regex_pattern": "GPT-?[0-9]+|Claude|Gemini",
    "max_results": 3,
    "check_comments": false,
    "headless": false
  }
}
```

## Key Changes:
- `headless: false` (default) - Opens visible Chrome window
- `headless: true` - Runs in background, no window
- Browser stays open for 5 seconds after completion in visible mode
- Shows "Browser window opened" message in visible mode


```json
{
  "input": {
    "search_term": "machine learning",
    "regex_pattern": "(deep|machine)\\s*(learning|neural)",
    "max_results": 10,
    "check_comments": true
  }
}
 ```§
