# Minitwit
Tested on:
- Ubuntu 22.04.4 x86-64
- gcc 11.4.0
- python 3.12, pip 25.0.1

## How to run Minitwit
1. Navigate to root of repository:
    ```bash
    cd path/to/repository
    ```
2. Create virtual environment:
    ```bash
    python -m venv .venv
    source .venv/bin/activate
    pip install -r requirements.txt
    ```
3. Create database (if it doesn't exist):
    ```bash
    ./control.sh init
    ```
4. Start application:
    ```bash
    ./control.sh start
    ```
5. Application can be reached at localhost:5000

## How to build flag_tool.c
1. Navigate to root of repository:
    ```bash
    cd path/to/repository
    ```
2. Build:
    ```bash
    make build
    ```

## How to create requirements.txt
1. Create new file 'requirements.txt' in root of repo
2. Insert this into the file:
    ```bash
    flask>=3.0.0
    werkzeug>=3.0.0
    jinja2>=3.0.0
    ```

## How to use 2to3
1. Create a new file minitwit.py with py3 syntax, to compare with the existing minitwit.py. By running this in the terminal:
    ```bash
    2to3 -n -w -o minitwit_py3 minitwit.py
    ```
   This adds a file 'minitwit.py' in the directory 'minitwit_py3'
2. Compare the two versions of minitwit.py, by running this in the terminal:
    ```bash
    diff minitwit.py minitwit_py3/minitwit.py
    ```
3. Observe the differences, and then substitute the old version for the new, by running this in the terminal:
    ```bash
    cp minitwit_py3/minitwit.py minitwit.py
    ```
   