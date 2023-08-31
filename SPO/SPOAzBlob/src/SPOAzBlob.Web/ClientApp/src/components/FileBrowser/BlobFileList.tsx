import { ContainerClient } from '@azure/storage-blob';
import React from 'react';
import { Component } from 'react';
import { ServiceConfiguration } from '../ConfigReader'
import { Dir } from './BlobItems/Dir'
import { File } from './BlobItems/File'
import { getActiveLocks, FileLock, BlobWithLock } from '../ApiLoader'

interface FileListProps {
    navToFolderCallback?: Function;
    accessToken: string,
    containerClient: ContainerClient,
    config: ServiceConfiguration
}
interface FileListState {
    blobItems: BlobWithLock[] | null,
    currentDirs: string[] | null,
    currentStoragePrefix: string,
    activeLocks: FileLock[] | null
}


export class BlobFileList extends Component<FileListProps, FileListState> {

    constructor(props: FileListProps) {
        super(props);
        this.state = { blobItems: null, currentDirs: null, currentStoragePrefix: "", activeLocks: null };
    }

    componentDidMount() {
        this.refreshFilesAndLocks();
    }

    refreshFilesAndLocks() {
        // Get locks from our own API
        getActiveLocks(this.props.accessToken).then((locks: FileLock[]) => {
            this.setState({ activeLocks: locks });

            // Get files for root
            this.listFilesFromBlobStorage(this.state.currentStoragePrefix).catch((error) => {
                // If there's going to be an error, it'll be now
                alert("Couldn't contact blob-storage. Is CORS configured for the account for this URL?")
                console.log(error);
            });
        });
    }

    // Dir nav functions
    setDir(clickedPrefix: string) {
        this.setState({ currentStoragePrefix: clickedPrefix });
        if (this.props.navToFolderCallback) {
            this.props.navToFolderCallback(clickedPrefix);
        }

        this.listFilesFromBlobStorage(clickedPrefix);
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
        this.setState({ currentStoragePrefix: newPath });
        this.listFilesFromBlobStorage(newPath);
    }

    // Find if an Azure blob has a lock
    findLockFor(relativeBlobName: string): FileLock | null {
        let l: FileLock | null = null;
        let azStorageAccountRoot = this.props.config.storageInfo.accountURI + this.props.config.storageInfo.containerName + "/";

        const blobNameFqdn = azStorageAccountRoot + relativeBlobName;

        if (this.state.activeLocks !== null) {
            this.state.activeLocks.forEach((lock: FileLock) => {
                
                if (decodeURI(lock.azureBlobUrl) === blobNameFqdn) {
                    l = lock;
                }
            });
        }
        return l;
    }

    // Call blobclient to enum files
    async listFilesFromBlobStorage(prefix: string) {

        let dirs: string[] = [];
        let blobs: BlobWithLock[] = [];

        try {
            // Get blobs for a specific prefix (dir)
            let iter = this.props.containerClient.listBlobsByHierarchy("/", { prefix: prefix });

            for await (const blob of iter) {

                // Sort dirs & blobs
                if (blob.kind === "prefix") {

                    // Just keep list of dir names
                    dirs.push(blob.name);
                } else {

                    // Try and find a lock for this blob, if there is one
                    const lock: FileLock | null = this.findLockFor(blob.name);
                    blobs.push({ blob: blob, lock: lock });
                }
            }

            this.setState({ blobItems: blobs, currentDirs: dirs });
            return Promise.resolve();
        } catch (error) {
            console.error(error);
            return Promise.reject(error);
        }
    }

    refreshAllLocks = () => {
        this.refreshFilesAndLocks();
    }

    render() {

        const breadcumbDirs = this.state.currentStoragePrefix.split("/") ?? "";

        return (
            <div>

                {this.state.currentDirs ?
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
                                        return <span key={dirIdx}>&gt;
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
                                return <Dir dir={dir} setDir={(dir: string) => this.setDir(dir)} key={dir} />
                                })
                            }
                            
                            {this.state.blobItems && this.state.blobItems.map(blobAndLock => {
                                return <File blobAndLock={blobAndLock} storageInfo={this.props.config.storageInfo}
                                    token={this.props.accessToken} refreshAllLocks={this.refreshAllLocks} key={blobAndLock.blob.name} />
                            })
                            }
                        </div>

                    </div>
                    :
                    <div>Loading files from blob storage API...</div>
                }
            </div>

        );
    }
}
