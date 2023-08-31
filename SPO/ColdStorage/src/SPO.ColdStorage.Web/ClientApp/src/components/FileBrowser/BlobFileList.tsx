import { BlobItem, ContainerClient } from '@azure/storage-blob';
import React from 'react';
import { Component } from 'react';
import { StorageInfo } from '../ConfigReader'

interface FileListProps {
    navToFolderCallback?: Function;
    accessToken: string,
    client: ContainerClient,
    storageInfo: StorageInfo
}
interface FileListState {
    blobItems: BlobItem[] | null,
    currentDirs: string[] | null,
    storagePrefix: string
}

export class BlobFileList extends Component<FileListProps, FileListState> {

    constructor(props: FileListProps)
    {
        super(props);
        this.state = { blobItems: null, currentDirs: null, storagePrefix: ""};
    }

    componentDidMount()
    {
        if (this.props.client) {
            this.listFiles("");
        }
    }

    setDir(clickedPrefix: string) {
        this.setState({storagePrefix: clickedPrefix});
        if (this.props.navToFolderCallback) {
            this.props.navToFolderCallback(clickedPrefix);
        }

        this.listFiles(clickedPrefix);
    }

    getDirName(fullName: string): string {
        const dirs = fullName.split("/");
        return dirs[dirs.length - 2];
    }
    getFileName(fullName: string): string {
        const dirs = fullName.split("/");
        return dirs[dirs.length - 1];
    }

    breadcrumbDirClick(dirIdx: number, allDirs: string[]) {
        let fullPath: string = "";

        for (let index = 0; index <= dirIdx; index++) {
            const thisDir = allDirs[index];
            fullPath += `${thisDir}/`;
        }
        this.setNewPath(fullPath);
    }
    setNewPath(newPath: string) {
        this.setState({storagePrefix: newPath});
        this.listFiles(newPath);
    }

    async listFiles(prefix: string) {

        let dirs: string[] = [];
        let blobs: BlobItem[] = [];

        try {
            let iter = this.props.client.listBlobsByHierarchy("/", { prefix: prefix });

            for await (const item of iter) {
                if (item.kind === "prefix") {
                    dirs.push(item.name);
                } else {
                    blobs.push(item);
                }
            }

            this.setState({ blobItems: blobs, currentDirs: dirs });

            return Promise.resolve();
        } catch (error) {
            return Promise.reject(error);
        }
    }

    getUrl(fileName: String) 
    {
        return this.props.storageInfo.accountURI + this.props.storageInfo.containerName + "/" + fileName 
            + this.props.storageInfo.sharedAccessToken;
    }

    render() {

        const breadcumbDirs = this.state.storagePrefix.split("/") ?? "";

        return (
            <div>
                <div id="breadcrumb-file-nav">
                    <span>
                        <span>
                            <button onClick={() => this.setNewPath("")} className="link-button">
                                Root
                            </button>
                        </span>
                        {breadcumbDirs && breadcumbDirs.map((breadcumbDir, dirIdx) => {
                            if (breadcumbDir) {
                                return <span>&gt;
                                    <button onClick={() => this.breadcrumbDirClick(dirIdx, breadcumbDirs)} className="link-button">
                                        {breadcumbDir}
                                    </button>
                                </span>
                            }
                            else
                                return <span />
                        })}
                    </span>
                </div>

                <div id="file-list">
                    {this.state.currentDirs && this.state.currentDirs.map(dir => {
                        return <div key={dir}>
                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-folder" viewBox="0 0 16 16">
                                <path d="M.54 3.87.5 3a2 2 0 0 1 2-2h3.672a2 2 0 0 1 1.414.586l.828.828A2 2 0 0 0 9.828 3h3.982a2 2 0 0 1 1.992 2.181l-.637 7A2 2 0 0 1 13.174 14H2.826a2 2 0 0 1-1.991-1.819l-.637-7a1.99 1.99 0 0 1 .342-1.31zM2.19 4a1 1 0 0 0-.996 1.09l.637 7a1 1 0 0 0 .995.91h10.348a1 1 0 0 0 .995-.91l.637-7A1 1 0 0 0 13.81 4H2.19zm4.69-1.707A1 1 0 0 0 6.172 2H2.5a1 1 0 0 0-1 .981l.006.139C1.72 3.042 1.95 3 2.19 3h5.396l-.707-.707z" />
                            </svg>
                            &nbsp;
                            <button className="link-button" onClick={() => this.setDir(dir)}>
                                {this.getDirName(dir)}
                            </button>
                        </div>
                    })
                    }
                    {this.state.blobItems && this.state.blobItems.map(blob => {
                        return <div key={blob.name}>
                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-file-earmark-text" viewBox="0 0 16 16">
                                <path d="M5.5 7a.5.5 0 0 0 0 1h5a.5.5 0 0 0 0-1h-5zM5 9.5a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5zm0 2a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5z" />
                                <path d="M9.5 0H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V4.5L9.5 0zm0 1v2A1.5 1.5 0 0 0 11 4.5h2V14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h5.5z" />
                            </svg>
                            <a href={this.getUrl(blob.name)}>
                                {this.getFileName(blob.name)}
                            </a>
                        </div>
                    })
                    }
                </div>
            </div>

        );
    }
}
